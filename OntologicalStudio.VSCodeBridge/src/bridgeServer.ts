import * as http from "http";
import * as vscode from "vscode";

export interface BridgeOptions {
    port: number;
    preferredModel?: string;
    output: vscode.OutputChannel;
}

interface ChatMessage {
    role: "system" | "user" | "assistant";
    content: string;
}

interface ChatPayload {
    messages?: ChatMessage[];
    systemPrompt?: string;
    userPrompt?: string;
    model?: string;
    temperature?: number;
    jsonMode?: boolean;
}

/**
 * Local HTTP server that bridges the OntologicalStudio desktop app to the
 * VSCode / TRAE Language Model API (vscode.lm). The desktop app POSTs prompts
 * to /chat and we forward them through the user's configured Copilot or other
 * chat model.
 *
 * The server only listens on 127.0.0.1 so it is not reachable from the network.
 */
export class BridgeServer {
    private server?: http.Server;
    private _running = false;
    private readonly opts: BridgeOptions;
    public lastUsedModel: string = "";
    public requestCount = 0;

    constructor(opts: BridgeOptions) {
        this.opts = opts;
    }

    get port(): number {
        return this.opts.port;
    }

    isRunning(): boolean {
        return this._running;
    }

    start(): Promise<void> {
        return new Promise((resolve, reject) => {
            this.server = http.createServer((req, res) => this.handle(req, res));
            this.server.on("error", (err) => {
                this.opts.output.appendLine(`[bridge] server error: ${err.message}`);
                reject(err);
            });
            this.server.listen(this.opts.port, "127.0.0.1", () => {
                this._running = true;
                resolve();
            });
        });
    }

    stop(): Promise<void> {
        return new Promise((resolve) => {
            if (!this.server) {
                this._running = false;
                resolve();
                return;
            }
            this.server.close(() => {
                this._running = false;
                this.server = undefined;
                resolve();
            });
        });
    }

    private async handle(req: http.IncomingMessage, res: http.ServerResponse): Promise<void> {
        // CORS for local desktop apps. Localhost only because of the bind.
        res.setHeader("Access-Control-Allow-Origin", "*");
        res.setHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
        res.setHeader("Access-Control-Allow-Headers", "Content-Type");

        if (req.method === "OPTIONS") {
            res.writeHead(204);
            res.end();
            return;
        }

        try {
            if (req.method === "GET" && req.url === "/health") {
                return this.json(res, 200, {
                    status: "ok",
                    bridge: "ontologicalstudio",
                    port: this.opts.port,
                    requestCount: this.requestCount,
                    lastUsedModel: this.lastUsedModel
                });
            }

            if (req.method === "GET" && req.url === "/models") {
                const models = await vscode.lm.selectChatModels({});
                return this.json(res, 200, {
                    models: models.map((m) => ({
                        id: m.id,
                        vendor: m.vendor,
                        family: m.family,
                        name: m.name,
                        version: m.version,
                        maxInputTokens: m.maxInputTokens
                    }))
                });
            }

            if (req.method === "POST" && req.url === "/chat") {
                const body = await this.readBody(req);
                let payload: ChatPayload;
                try {
                    payload = JSON.parse(body || "{}");
                } catch {
                    return this.json(res, 400, { error: "Invalid JSON body" });
                }
                const result = await this.runChat(payload);
                this.requestCount += 1;
                return this.json(res, 200, result);
            }

            this.json(res, 404, { error: "Not found" });
        } catch (err) {
            const message = err instanceof Error ? err.message : String(err);
            this.opts.output.appendLine(`[bridge] handler error: ${message}`);
            this.json(res, 500, { error: message });
        }
    }

    private async runChat(payload: ChatPayload): Promise<{ content: string; model: string }> {
        const messages = this.buildMessages(payload);
        if (messages.length === 0) {
            throw new Error("No messages provided in request body.");
        }

        const requestedModel = (payload.model || this.opts.preferredModel || "").trim();
        const filter = requestedModel ? this.modelFilter(requestedModel) : {};
        let models = await vscode.lm.selectChatModels(filter);
        if (models.length === 0 && requestedModel) {
            this.opts.output.appendLine(`[bridge] requested model "${requestedModel}" not found, falling back to default`);
            models = await vscode.lm.selectChatModels({});
        }
        if (models.length === 0) {
            throw new Error(
                "No chat models are available. Install or sign in to GitHub Copilot (or another chat provider) in VSCode/TRAE."
            );
        }

        const model = models[0];
        this.lastUsedModel = `${model.vendor}/${model.family}`;
        this.opts.output.appendLine(`[bridge] dispatch -> ${this.lastUsedModel} (${model.id})`);

        const lmMessages = messages.map((m) =>
            m.role === "assistant"
                ? vscode.LanguageModelChatMessage.Assistant(m.content)
                : vscode.LanguageModelChatMessage.User(
                      m.role === "system" ? `[SYSTEM]\n${m.content}` : m.content
                  )
        );

        const cts = new vscode.CancellationTokenSource();
        const timeout = setTimeout(() => cts.cancel(), 120_000);
        try {
            const response = await model.sendRequest(lmMessages, {}, cts.token);
            let content = "";
            for await (const chunk of response.text) {
                content += chunk;
            }
            return { content, model: this.lastUsedModel };
        } finally {
            clearTimeout(timeout);
            cts.dispose();
        }
    }

    private buildMessages(payload: ChatPayload): ChatMessage[] {
        if (Array.isArray(payload.messages) && payload.messages.length > 0) {
            return payload.messages.filter((m) => m && typeof m.content === "string");
        }
        const out: ChatMessage[] = [];
        if (payload.systemPrompt && payload.systemPrompt.trim()) {
            out.push({ role: "system", content: payload.systemPrompt });
        }
        if (payload.userPrompt && payload.userPrompt.trim()) {
            out.push({ role: "user", content: payload.userPrompt });
        }
        return out;
    }

    private modelFilter(requested: string): vscode.LanguageModelChatSelector {
        // Accept "vendor/family", "family", or "id"
        const parts = requested.split("/");
        if (parts.length === 2) {
            return { vendor: parts[0], family: parts[1] };
        }
        return { family: requested };
    }

    private json(res: http.ServerResponse, status: number, body: unknown): void {
        res.writeHead(status, { "Content-Type": "application/json" });
        res.end(JSON.stringify(body));
    }

    private readBody(req: http.IncomingMessage): Promise<string> {
        return new Promise((resolve, reject) => {
            const chunks: Buffer[] = [];
            req.on("data", (c: Buffer) => chunks.push(c));
            req.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
            req.on("error", reject);
        });
    }
}

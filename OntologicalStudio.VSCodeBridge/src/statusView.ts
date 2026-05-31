import * as vscode from "vscode";
import type { BridgeServer } from "./bridgeServer";

/**
 * Small status webview shown in the Ontological Studio activity-bar container.
 * Lets the user start/stop the bridge and open the main panel without using
 * the command palette.
 */
export class StatusViewProvider implements vscode.WebviewViewProvider {
    public static readonly viewType = "ontologicalstudio.statusView";
    private view?: vscode.WebviewView;

    constructor(
        private readonly _extensionUri: vscode.Uri,
        private readonly getBridge: () => BridgeServer | undefined
    ) {}

    resolveWebviewView(webviewView: vscode.WebviewView): void {
        this.view = webviewView;
        webviewView.webview.options = { enableScripts: true };
        webviewView.webview.html = this.render();
        webviewView.webview.onDidReceiveMessage(async (msg) => {
            switch (msg.type) {
                case "start":
                    await vscode.commands.executeCommand("ontologicalstudio.startBridge");
                    this.refresh();
                    return;
                case "stop":
                    await vscode.commands.executeCommand("ontologicalstudio.stopBridge");
                    this.refresh();
                    return;
                case "openPanel":
                    await vscode.commands.executeCommand("ontologicalstudio.openPanel");
                    return;
                case "refresh":
                    this.refresh();
                    return;
            }
        });
    }

    public refresh(): void {
        if (this.view) {
            this.view.webview.html = this.render();
        }
    }

    private render(): string {
        const bridge = this.getBridge();
        const running = bridge?.isRunning() ?? false;
        const port = bridge?.port ?? 39217;
        const lastModel = bridge?.lastUsedModel || "—";
        const requests = bridge?.requestCount ?? 0;

        const dot = running ? "#3ad17a" : "#777";
        const label = running ? "Running" : "Stopped";

        return /* html */ `<!DOCTYPE html>
<html><head><meta charset="utf-8" />
<style>
body { font-family: var(--vscode-font-family); color: var(--vscode-foreground); padding: 10px; }
.row { display: flex; align-items: center; gap: 8px; margin: 6px 0; font-size: 12px; }
.dot { width: 10px; height: 10px; border-radius: 50%; background: ${dot}; box-shadow: 0 0 6px ${dot}55; }
.kv { display: flex; justify-content: space-between; font-size: 11px; color: var(--vscode-descriptionForeground); }
button { width: 100%; margin-top: 6px; padding: 6px 8px; border: 1px solid var(--vscode-button-border, #444);
         background: var(--vscode-button-background, #2c2c2c); color: var(--vscode-button-foreground, #fff);
         border-radius: 4px; cursor: pointer; font-size: 12px; }
button:hover { background: var(--vscode-button-hoverBackground, #3a3a3a); }
hr { border: none; border-top: 1px solid var(--vscode-panel-border, #333); margin: 10px 0; }
.muted { color: var(--vscode-descriptionForeground); font-size: 11px; }
</style></head>
<body>
  <div class="row"><span class="dot"></span><strong>${label}</strong></div>
  <div class="kv"><span>Port</span><span>${port}</span></div>
  <div class="kv"><span>Last model</span><span>${lastModel}</span></div>
  <div class="kv"><span>Requests</span><span>${requests}</span></div>
  <hr/>
  ${
      running
          ? `<button onclick="post('stop')">Stop bridge</button>`
          : `<button onclick="post('start')">Start bridge</button>`
  }
  <button onclick="post('openPanel')">Open Ontological Studio panel</button>
  <button onclick="post('refresh')" style="background:transparent;border-color:var(--vscode-panel-border,#333)">Refresh</button>
  <hr/>
  <div class="muted">
    The desktop app should be configured to use<br/>
    <code>http://localhost:${port}</code> as its provider endpoint
    with provider type <strong>VSCode / TRAE Bridge</strong>.
  </div>
<script>
  const vscode = acquireVsCodeApi();
  function post(type) { vscode.postMessage({ type }); }
</script>
</body></html>`;
    }
}

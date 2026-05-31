import * as http from "http";
import { URL } from "url";

/**
 * Minimal HTTP client for the OntologicalStudio.Api running on localhost.
 * Avoids any third-party dependency.
 */
export class DesktopApiClient {
    constructor(private readonly baseUrl: string) {}

    async get<T>(path: string): Promise<T> {
        const url = new URL(path, this.baseUrl);
        return new Promise<T>((resolve, reject) => {
            const req = http.request(
                {
                    method: "GET",
                    hostname: url.hostname,
                    port: url.port || 80,
                    path: url.pathname + url.search,
                    headers: { Accept: "application/json" }
                },
                (res) => {
                    const chunks: Buffer[] = [];
                    res.on("data", (c: Buffer) => chunks.push(c));
                    res.on("end", () => {
                        const body = Buffer.concat(chunks).toString("utf8");
                        if (!res.statusCode || res.statusCode >= 400) {
                            reject(new Error(`GET ${path} -> ${res.statusCode}: ${body}`));
                            return;
                        }
                        try {
                            resolve(body ? (JSON.parse(body) as T) : (undefined as unknown as T));
                        } catch (err) {
                            reject(err);
                        }
                    });
                }
            );
            req.on("error", reject);
            req.end();
        });
    }
}

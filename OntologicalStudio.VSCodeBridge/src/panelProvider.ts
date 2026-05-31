import * as vscode from "vscode";
import { DesktopApiClient } from "./apiClient";

interface Universe {
    id: string;
    name: string;
    description?: string;
}

interface Scenario {
    id: string;
    title: string;
    description?: string;
    status?: string;
}

interface Solution {
    id: string;
    title?: string;
    providerUsed?: string;
    status?: string;
    artifacts?: Array<{
        id: string;
        kind: string;
        label: string;
        mimeType?: string;
        inlineContent?: string;
    }>;
}

/**
 * Webview panel that mirrors the Ontological Studio desktop UI (read-only) by
 * polling the local desktop API. Useful for staying inside VSCode / TRAE while
 * the desktop app runs in the background.
 */
export class PanelProvider {
    public static readonly viewType = "ontologicalstudio.panel";
    private static currentPanel: PanelProvider | undefined;

    private readonly panel: vscode.WebviewPanel;
    private readonly disposables: vscode.Disposable[] = [];
    private api: DesktopApiClient;

    private constructor(panel: vscode.WebviewPanel, extensionUri: vscode.Uri) {
        this.panel = panel;
        const baseUrl = vscode.workspace
            .getConfiguration("ontologicalstudio.desktop")
            .get<string>("apiBaseUrl", "http://127.0.0.1:53821");
        this.api = new DesktopApiClient(baseUrl);

        this.panel.webview.html = this.renderHtml(extensionUri);
        this.panel.onDidDispose(() => this.dispose(), null, this.disposables);
        this.panel.webview.onDidReceiveMessage(
            async (msg) => this.onMessage(msg),
            null,
            this.disposables
        );
    }

    public static async createOrShow(extensionUri: vscode.Uri): Promise<void> {
        if (PanelProvider.currentPanel) {
            PanelProvider.currentPanel.panel.reveal(vscode.ViewColumn.Beside);
            await PanelProvider.currentPanel.refresh();
            return;
        }
        const panel = vscode.window.createWebviewPanel(
            PanelProvider.viewType,
            "Ontological Studio",
            vscode.ViewColumn.Beside,
            {
                enableScripts: true,
                retainContextWhenHidden: true
            }
        );
        PanelProvider.currentPanel = new PanelProvider(panel, extensionUri);
        await PanelProvider.currentPanel.refresh();
    }

    private async onMessage(msg: { type: string; payload?: unknown }): Promise<void> {
        try {
            switch (msg.type) {
                case "refresh":
                    await this.refresh();
                    return;
                case "loadScenarios": {
                    const universeId = (msg.payload as { universeId: string }).universeId;
                    const scenarios = await this.api.get<Scenario[]>(
                        `/api/universes/${universeId}/scenarios`
                    );
                    this.panel.webview.postMessage({ type: "scenariosLoaded", scenarios, universeId });
                    return;
                }
                case "loadSolutions": {
                    const scenarioId = (msg.payload as { scenarioId: string }).scenarioId;
                    const solutions = await this.api.get<Solution[]>(
                        `/api/scenarios/${scenarioId}/solutions`
                    );
                    this.panel.webview.postMessage({ type: "solutionsLoaded", solutions, scenarioId });
                    return;
                }
                case "loadSolution": {
                    const solutionId = (msg.payload as { solutionId: string }).solutionId;
                    const solution = await this.api.get<Solution>(`/api/solutions/${solutionId}`);
                    this.panel.webview.postMessage({ type: "solutionLoaded", solution });
                    return;
                }
            }
        } catch (err) {
            this.panel.webview.postMessage({
                type: "error",
                message: err instanceof Error ? err.message : String(err)
            });
        }
    }

    private async refresh(): Promise<void> {
        try {
            const universes = await this.api.get<Universe[]>("/api/universes");
            this.panel.webview.postMessage({ type: "universesLoaded", universes });
        } catch (err) {
            this.panel.webview.postMessage({
                type: "error",
                message:
                    `Could not reach the desktop API. Is the Ontological Studio desktop app running?\n` +
                    (err instanceof Error ? err.message : String(err))
            });
        }
    }

    private dispose(): void {
        PanelProvider.currentPanel = undefined;
        this.panel.dispose();
        while (this.disposables.length) {
            const d = this.disposables.pop();
            if (d) d.dispose();
        }
    }

    private renderHtml(_extensionUri: vscode.Uri): string {
        const nonce = Math.random().toString(36).slice(2);
        return /* html */ `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta http-equiv="Content-Security-Policy"
      content="default-src 'none'; style-src 'unsafe-inline'; script-src 'nonce-${nonce}';" />
<title>Ontological Studio</title>
<style>
:root {
  --bg: var(--vscode-editor-background, #1e1e1e);
  --fg: var(--vscode-editor-foreground, #d4d4d4);
  --muted: var(--vscode-descriptionForeground, #9ca3af);
  --border: var(--vscode-panel-border, #333);
  --accent: var(--vscode-textLink-foreground, #4ea0ff);
  --card: rgba(255,255,255,0.03);
}
body { font-family: var(--vscode-font-family); background: var(--bg); color: var(--fg); margin: 0; padding: 16px; }
header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
header h1 { font-size: 14px; font-weight: 600; margin: 0; letter-spacing: 0.3px; }
button { background: transparent; color: var(--fg); border: 1px solid var(--border);
         padding: 4px 10px; border-radius: 4px; cursor: pointer; font-size: 12px; }
button:hover { background: rgba(255,255,255,0.05); }
.layout { display: grid; grid-template-columns: 1fr 1fr 1.4fr; gap: 12px; height: calc(100vh - 80px); }
.col { display: flex; flex-direction: column; border: 1px solid var(--border); border-radius: 6px; overflow: hidden; }
.col-header { padding: 8px 10px; font-size: 11px; text-transform: uppercase; letter-spacing: 0.8px;
              color: var(--muted); border-bottom: 1px solid var(--border); background: var(--card); }
.col-body { overflow-y: auto; flex: 1; padding: 6px; }
.item { padding: 8px 10px; border-radius: 4px; cursor: pointer; margin-bottom: 4px; border: 1px solid transparent; }
.item:hover { background: rgba(255,255,255,0.04); }
.item.active { background: rgba(78,160,255,0.12); border-color: rgba(78,160,255,0.35); }
.item .title { font-weight: 500; font-size: 13px; }
.item .sub { color: var(--muted); font-size: 11px; margin-top: 2px; }
.empty { color: var(--muted); padding: 12px; font-size: 12px; font-style: italic; }
.error { color: #ff7676; padding: 10px; font-size: 12px; white-space: pre-wrap; border: 1px solid rgba(255,118,118,0.3);
         border-radius: 4px; background: rgba(255,118,118,0.06); margin-bottom: 12px; }
.artifact { margin-bottom: 10px; border: 1px solid var(--border); border-radius: 6px; overflow: hidden; }
.artifact-head { padding: 6px 10px; background: var(--card); border-bottom: 1px solid var(--border);
                 display: flex; gap: 8px; align-items: center; }
.artifact-head .label { font-weight: 600; font-size: 12px; }
.artifact-head .kind { font-size: 10px; color: var(--muted); text-transform: uppercase; }
.artifact-body { padding: 10px; font-family: var(--vscode-editor-font-family, monospace); font-size: 12px;
                 white-space: pre-wrap; max-height: 300px; overflow-y: auto; }
.badge { display: inline-block; padding: 1px 6px; font-size: 10px; border-radius: 10px;
         background: rgba(78,160,255,0.15); color: var(--accent); margin-left: 6px; }
</style>
</head>
<body>
<header>
  <h1>🌐 Ontological Studio · VSCode/TRAE Panel</h1>
  <div>
    <button id="refresh">Refresh</button>
  </div>
</header>
<div id="err"></div>
<div class="layout">
  <div class="col">
    <div class="col-header">Universes</div>
    <div class="col-body" id="universes"><div class="empty">Loading…</div></div>
  </div>
  <div class="col">
    <div class="col-header">Scenarios</div>
    <div class="col-body" id="scenarios"><div class="empty">Pick a universe</div></div>
  </div>
  <div class="col">
    <div class="col-header">Solutions &amp; Artifacts</div>
    <div class="col-body" id="solutions"><div class="empty">Pick a scenario</div></div>
  </div>
</div>
<script nonce="${nonce}">
const vscode = acquireVsCodeApi();
const $u = document.getElementById('universes');
const $s = document.getElementById('scenarios');
const $sol = document.getElementById('solutions');
const $err = document.getElementById('err');
let selectedUniverse = null;
let selectedScenario = null;

document.getElementById('refresh').addEventListener('click', () => {
  $err.innerHTML = '';
  vscode.postMessage({ type: 'refresh' });
});

function renderItems(container, items, onClick, activeId, fmt) {
  container.innerHTML = '';
  if (!items || items.length === 0) {
    container.innerHTML = '<div class="empty">Nothing here yet.</div>';
    return;
  }
  for (const it of items) {
    const div = document.createElement('div');
    div.className = 'item' + (it.id === activeId ? ' active' : '');
    div.innerHTML = fmt(it);
    div.addEventListener('click', () => onClick(it));
    container.appendChild(div);
  }
}

window.addEventListener('message', (event) => {
  const msg = event.data;
  if (msg.type === 'universesLoaded') {
    renderItems($u, msg.universes, (u) => {
      selectedUniverse = u.id;
      selectedScenario = null;
      $sol.innerHTML = '<div class="empty">Pick a scenario</div>';
      vscode.postMessage({ type: 'loadScenarios', payload: { universeId: u.id } });
      // re-render to highlight
      renderItems($u, msg.universes, () => {}, selectedUniverse, fmtUniverse);
    }, selectedUniverse, fmtUniverse);
  }
  if (msg.type === 'scenariosLoaded') {
    renderItems($s, msg.scenarios, (sc) => {
      selectedScenario = sc.id;
      vscode.postMessage({ type: 'loadSolutions', payload: { scenarioId: sc.id } });
      renderItems($s, msg.scenarios, () => {}, selectedScenario, fmtScenario);
    }, selectedScenario, fmtScenario);
  }
  if (msg.type === 'solutionsLoaded') {
    const sols = msg.solutions || [];
    if (sols.length === 0) {
      $sol.innerHTML = '<div class="empty">No solutions for this scenario.</div>';
      return;
    }
    $sol.innerHTML = '';
    for (const s of sols) {
      const wrap = document.createElement('div');
      wrap.className = 'artifact';
      wrap.innerHTML =
        '<div class="artifact-head"><span class="label">' + escapeHtml(s.title || 'Untitled') + '</span>' +
        '<span class="kind">' + escapeHtml(s.status || '') + '</span>' +
        (s.providerUsed ? '<span class="badge">' + escapeHtml(s.providerUsed) + '</span>' : '') +
        '<button data-id="' + s.id + '" style="margin-left:auto">View artifacts</button></div>' +
        '<div class="artifact-body" id="sol-body-' + s.id + '"><div class="empty">Click "View artifacts"</div></div>';
      $sol.appendChild(wrap);
    }
    $sol.querySelectorAll('button[data-id]').forEach((btn) => {
      btn.addEventListener('click', () => {
        vscode.postMessage({ type: 'loadSolution', payload: { solutionId: btn.dataset.id } });
      });
    });
  }
  if (msg.type === 'solutionLoaded') {
    const sol = msg.solution;
    const body = document.getElementById('sol-body-' + sol.id);
    if (!body) return;
    if (!sol.artifacts || sol.artifacts.length === 0) {
      body.innerHTML = '<div class="empty">No artifacts.</div>';
      return;
    }
    body.innerHTML = sol.artifacts.map(a =>
      '<div class="artifact"><div class="artifact-head"><span class="label">' + escapeHtml(a.label) +
      '</span><span class="kind">' + escapeHtml(a.kind) + '</span></div>' +
      '<div class="artifact-body">' + escapeHtml(a.inlineContent || '(no inline content)') + '</div></div>'
    ).join('');
  }
  if (msg.type === 'error') {
    $err.innerHTML = '<div class="error">' + escapeHtml(msg.message) + '</div>';
  }
});

function fmtUniverse(u) {
  return '<div class="title">' + escapeHtml(u.name) + '</div>' +
    (u.description ? '<div class="sub">' + escapeHtml(u.description) + '</div>' : '');
}
function fmtScenario(s) {
  return '<div class="title">' + escapeHtml(s.title) + '</div>' +
    '<div class="sub">' + escapeHtml(s.status || '') + '</div>';
}
function escapeHtml(s) {
  return String(s || '').replace(/[&<>"']/g, (c) => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
}

vscode.postMessage({ type: 'refresh' });
</script>
</body>
</html>`;
    }
}

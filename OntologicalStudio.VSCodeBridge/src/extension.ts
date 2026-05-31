import * as vscode from "vscode";
import { BridgeServer } from "./bridgeServer";
import { PanelProvider } from "./panelProvider";
import { StatusViewProvider } from "./statusView";

let bridge: BridgeServer | undefined;
let statusBarItem: vscode.StatusBarItem;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    const output = vscode.window.createOutputChannel("Ontological Studio Bridge");
    context.subscriptions.push(output);

    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
    statusBarItem.command = "ontologicalstudio.showStatus";
    context.subscriptions.push(statusBarItem);

    const refreshStatusBar = () => {
        if (bridge?.isRunning()) {
            statusBarItem.text = `$(sync) OS Bridge :${bridge.port}`;
            statusBarItem.tooltip = "Ontological Studio AI Bridge is running. Click for details.";
        } else {
            statusBarItem.text = "$(circle-slash) OS Bridge off";
            statusBarItem.tooltip = "Ontological Studio AI Bridge is stopped. Click for details.";
        }
        statusBarItem.show();
    };

    const statusViewProvider = new StatusViewProvider(context.extensionUri, () => bridge);
    context.subscriptions.push(
        vscode.window.registerWebviewViewProvider(StatusViewProvider.viewType, statusViewProvider)
    );

    const startBridge = async () => {
        if (bridge?.isRunning()) {
            vscode.window.showInformationMessage(`Ontological Studio Bridge is already running on port ${bridge.port}.`);
            return;
        }
        const config = vscode.workspace.getConfiguration("ontologicalstudio.bridge");
        const port = config.get<number>("port", 39217);
        const preferredModel = config.get<string>("preferredModel", "");

        bridge = new BridgeServer({ port, preferredModel, output });
        try {
            await bridge.start();
            output.appendLine(`[bridge] started on http://localhost:${port}`);
            vscode.window.showInformationMessage(`Ontological Studio Bridge running on port ${port}.`);
        } catch (err) {
            output.appendLine(`[bridge] start failed: ${(err as Error).message}`);
            vscode.window.showErrorMessage(`Failed to start bridge: ${(err as Error).message}`);
            bridge = undefined;
        }
        refreshStatusBar();
        statusViewProvider.refresh();
    };

    const stopBridge = async () => {
        if (!bridge?.isRunning()) {
            vscode.window.showInformationMessage("Bridge is not running.");
            return;
        }
        await bridge.stop();
        output.appendLine("[bridge] stopped");
        vscode.window.showInformationMessage("Ontological Studio Bridge stopped.");
        bridge = undefined;
        refreshStatusBar();
        statusViewProvider.refresh();
    };

    const showStatus = async () => {
        const running = bridge?.isRunning();
        const port = bridge?.port;
        const lastModel = bridge?.lastUsedModel ?? "(none)";
        const requests = bridge?.requestCount ?? 0;
        const msg = running
            ? `Bridge ON · port ${port} · last model: ${lastModel} · requests served: ${requests}`
            : "Bridge OFF. Use 'Ontological Studio: Start AI Bridge'.";
        const action = running ? "Stop" : "Start";
        const choice = await vscode.window.showInformationMessage(msg, action, "Open Panel", "Open Logs");
        if (choice === "Start") await startBridge();
        if (choice === "Stop") await stopBridge();
        if (choice === "Open Panel") await vscode.commands.executeCommand("ontologicalstudio.openPanel");
        if (choice === "Open Logs") output.show();
    };

    const openPanel = async () => {
        await PanelProvider.createOrShow(context.extensionUri);
    };

    context.subscriptions.push(
        vscode.commands.registerCommand("ontologicalstudio.startBridge", startBridge),
        vscode.commands.registerCommand("ontologicalstudio.stopBridge", stopBridge),
        vscode.commands.registerCommand("ontologicalstudio.showStatus", showStatus),
        vscode.commands.registerCommand("ontologicalstudio.openPanel", openPanel),
        { dispose: () => bridge?.stop() }
    );

    const autoStart = vscode.workspace.getConfiguration("ontologicalstudio.bridge").get<boolean>("autoStart", true);
    if (autoStart) {
        await startBridge();
    } else {
        refreshStatusBar();
    }
}

export async function deactivate(): Promise<void> {
    if (bridge?.isRunning()) {
        await bridge.stop();
    }
}

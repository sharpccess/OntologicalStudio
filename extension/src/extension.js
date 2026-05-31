const vscode = require('vscode');

const API_BASE = 'http://127.0.0.1:53821';

async function fetchJson(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`${response.status} ${response.statusText}: ${text}`);
  }

  return response.json();
}

function activate(context) {
  context.subscriptions.push(vscode.commands.registerCommand('ontologicalStudio.healthCheck', async () => {
    try {
      const health = await fetchJson('/health');
      vscode.window.showInformationMessage(`Ontological Studio API: ${health.status}`);
    } catch (error) {
      vscode.window.showErrorMessage(`Health check failed: ${error.message}`);
    }
  }));

  context.subscriptions.push(vscode.commands.registerCommand('ontologicalStudio.listUniverses', async () => {
    try {
      const universes = await fetchJson('/api/universes');
      if (!universes.length) {
        vscode.window.showInformationMessage('No universes found.');
        return;
      }

      const pick = await vscode.window.showQuickPick(
        universes.map(u => ({
          label: u.name,
          description: u.description,
          detail: u.id
        })),
        { placeHolder: 'Select a universe' }
      );

      if (pick) {
        vscode.window.showInformationMessage(`Selected universe: ${pick.label}`);
      }
    } catch (error) {
      vscode.window.showErrorMessage(`List universes failed: ${error.message}`);
    }
  }));

  context.subscriptions.push(vscode.commands.registerCommand('ontologicalStudio.solveScenario', async () => {
    try {
      const universes = await fetchJson('/api/universes');
      if (!universes.length) {
        vscode.window.showInformationMessage('No universes found.');
        return;
      }

      const universePick = await vscode.window.showQuickPick(
        universes.map(u => ({
          label: u.name,
          description: u.description,
          detail: u.id
        })),
        { placeHolder: 'Select a universe' }
      );

      if (!universePick) {
        return;
      }

      const scenarios = await fetchJson(`/api/universes/${universePick.detail}/scenarios`);
      if (!scenarios.length) {
        vscode.window.showInformationMessage('This universe has no scenarios.');
        return;
      }

      const scenarioPick = await vscode.window.showQuickPick(
        scenarios.map(s => ({
          label: s.title,
          description: s.description,
          detail: s.id
        })),
        { placeHolder: 'Select a scenario' }
      );

      if (!scenarioPick) {
        return;
      }

      const extraInstructions = await vscode.window.showInputBox({
        prompt: 'Optional extra instructions for the AI solution'
      });

      const result = await fetchJson(`/api/scenarios/${scenarioPick.detail}/solve`, {
        method: 'POST',
        body: JSON.stringify({ extraInstructions: extraInstructions || null })
      });

      const firstArtifact = result.artifacts?.[0]?.inlineContent || 'No artifact returned.';
      const doc = await vscode.workspace.openTextDocument({
        language: 'markdown',
        content: firstArtifact
      });
      await vscode.window.showTextDocument(doc, { preview: false });
    } catch (error) {
      vscode.window.showErrorMessage(`Solve scenario failed: ${error.message}`);
    }
  }));
}

function deactivate() {}

module.exports = {
  activate,
  deactivate
};
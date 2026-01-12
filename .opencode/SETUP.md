# OpenCode Setup Guide

This guide covers installing and configuring MCP servers and LSP for the hexgame project.

## Prerequisites

- Node.js 20+
- npm or pnpm
- OpenCode CLI installed

## MCP Server Installation

MCP (Model Context Protocol) servers provide tools for AI agents. The servers are configured in `mcp.json` and run via npx (no global install needed).

### Required MCP Servers

| Server | Purpose | Install |
|--------|---------|---------|
| filesystem | Read/write project files | Auto via npx |
| git | Version control operations | Auto via npx |
| github | PR/issue management | Auto via npx |
| fetch | Web content fetching | Auto via npx |
| memory | Persistent context | Auto via npx |

### Manual Installation (Optional)

If you prefer global installation:

```bash
# Filesystem server
npm install -g @anthropic/mcp-server-filesystem

# Git server
npm install -g @anthropic/mcp-server-git

# GitHub server (requires GITHUB_TOKEN env var)
npm install -g @anthropic/mcp-server-github

# Fetch server
npm install -g @anthropic/mcp-server-fetch

# Memory server
npm install -g @anthropic/mcp-server-memory
```

### GitHub Server Setup

The GitHub MCP server requires authentication:

1. Create a GitHub Personal Access Token:
   - Go to https://github.com/settings/tokens
   - Click "Generate new token (classic)"
   - Select scopes: `repo`, `read:org`
   - Copy the token

2. Set environment variable:
   ```bash
   # Linux/macOS
   export GITHUB_TOKEN=ghp_your_token_here

   # Windows (PowerShell)
   $env:GITHUB_TOKEN = "ghp_your_token_here"

   # Windows (CMD)
   set GITHUB_TOKEN=ghp_your_token_here
   ```

3. Or add to your shell profile (~/.bashrc, ~/.zshrc, etc.):
   ```bash
   export GITHUB_TOKEN=ghp_your_token_here
   ```

### Verifying MCP Servers

Test that servers work:

```bash
# Test filesystem server
npx -y @anthropic/mcp-server-filesystem .

# Test git server
npx -y @anthropic/mcp-server-git .
```

## LSP (Language Server Protocol) Setup

LSP provides IDE features like autocomplete, error checking, and go-to-definition.

### TypeScript Language Server

The TypeScript language server is the primary LSP for this project.

#### Installation

```bash
# Global installation
npm install -g typescript-language-server typescript

# Or project-local (already in devDependencies)
npm install
```

#### VS Code

VS Code has built-in TypeScript support. No additional setup needed.

The `.vscode/settings.json` configures:
```json
{
  "typescript.tsdk": "node_modules/typescript/lib"
}
```

#### Neovim (with nvim-lspconfig)

```lua
require('lspconfig').ts_ls.setup({
  root_dir = require('lspconfig.util').root_pattern('tsconfig.json', 'package.json'),
  settings = {
    typescript = {
      inlayHints = {
        includeInlayParameterNameHints = 'all',
      },
    },
  },
})
```

#### Helix

In `~/.config/helix/languages.toml`:

```toml
[[language]]
name = "typescript"
language-servers = ["typescript-language-server"]
```

#### Emacs (with lsp-mode)

```elisp
(use-package lsp-mode
  :hook ((typescript-mode . lsp)
         (typescript-tsx-mode . lsp))
  :commands lsp)
```

### GLSL Language Server (Optional)

For shader development:

```bash
npm install -g @vscode/vscode-languageserver-glsl
```

## OpenCode Configuration

### Loading the Configuration

OpenCode automatically loads `.opencode/config.yaml` when started in the project directory.

### Testing the Setup

1. Start OpenCode in the project:
   ```bash
   cd /path/to/hexgame
   opencode
   ```

2. Verify MCP servers are loaded:
   ```
   /mcp status
   ```

3. Test a skill:
   ```
   /run-tests
   ```

## Troubleshooting

### MCP Server Won't Start

1. Check Node.js version: `node --version` (need 20+)
2. Clear npx cache: `npx clear-npx-cache`
3. Try manual install: `npm install -g @anthropic/mcp-server-filesystem`

### GitHub Server Authentication Failed

1. Verify token is set: `echo $GITHUB_TOKEN`
2. Check token permissions on GitHub
3. Regenerate token if expired

### TypeScript LSP Not Working

1. Ensure `tsconfig.json` exists in project root
2. Run `npm install` to get local TypeScript
3. Restart your editor/LSP client

### Memory Server Permissions

If memory server can't write to `.opencode/memory`:

```bash
mkdir -p .opencode/memory
chmod 755 .opencode/memory
```

## Resources

- [MCP Documentation](https://modelcontextprotocol.io/)
- [TypeScript Language Server](https://github.com/typescript-language-server/typescript-language-server)
- [OpenCode Documentation](https://opencode.ai/docs)

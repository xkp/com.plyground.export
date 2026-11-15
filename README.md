# Plyground Unity exporter

The package includes a snapshot of the Plythreejs Preview tools under
`Tools~/plythreejs`. Unity ignores this folder during asset import. When a
matching Plyground Desktop toolchain is installed, the exporter uses it;
otherwise it copies the bundled snapshot to the user's local cache and runs
`npm ci --omit=dev` there on the first Preview export. A standalone Unity
client needs Node.js with npm and the .NET 10 SDK to export Preview data.
The snapshot excludes Plythreejs example compatibility fixtures, including
the JU and RedMono animation archives.

To update the bundled snapshot before releasing this package:

```powershell
node scripts/sync-plythreejs.mjs C:\path\to\plythreejs
```

Commit the resulting `Tools~/plythreejs` files with the exporter changes. The
bundle manifest records a source hash. Local development can override the
bundle with `PLYGROUND_PREVIEW_TOOL_ROOT` when developing Plythreejs.

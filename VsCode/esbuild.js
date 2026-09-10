const esbuild = require('esbuild');

const watch = process.argv.includes('--watch');

/** @type {import('esbuild').BuildOptions} */
const options = {
  entryPoints: ['src/extension.ts'],
  bundle: true,
  format: 'cjs',
  platform: 'node',
  target: 'node18',
  outfile: 'out/extension.js',
  external: ['vscode'],
  sourcemap: true,
  sourcesContent: false,
  minify: false,
  metafile: process.argv.includes('--metafile'),
  logLevel: 'info',
};

async function main() {
  if (watch) {
    const ctx = await esbuild.context(options);
    await ctx.watch();
  } else {
    const result = await esbuild.build(options);
    if (options.metafile) {
      const files = Object.keys(result.metafile.outputs);
      console.log('outputs:', files.join(', '));
    }
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});

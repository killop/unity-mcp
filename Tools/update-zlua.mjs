#!/usr/bin/env node

import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const repository = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const destination = path.join(repository, 'Packages', 'com.code-philosophy.zlua');
const args = process.argv.slice(2);
const check = args.includes('--check');
const sourceOption = args.indexOf('--source');
if (sourceOption >= 0 && !args[sourceOption + 1]) {
  throw new Error('--source requires a directory.');
}
const source = path.resolve(sourceOption >= 0
  ? args[sourceOption + 1]
  : 'D:/github/focus-creative-games/zlua');
const allowed = new Set(['--check', '--source']);
for (let i = 0; i < args.length; i++) {
  if (args[i] === '--source') { i++; continue; }
  if (!allowed.has(args[i])) throw new Error(`Unknown argument: ${args[i]}`);
}

function git(...command) {
  const result = spawnSync('git', ['-C', source, ...command], { encoding: 'utf8' });
  if (result.status !== 0) throw new Error(result.stderr.trim() || `git ${command.join(' ')} failed`);
  return result.stdout.trim();
}

function files(root) {
  const found = new Map();
  function visit(directory, relative) {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      if (entry.name === '.git') continue;
      const nextRelative = relative ? path.join(relative, entry.name) : entry.name;
      const fullPath = path.join(directory, entry.name);
      if (entry.isSymbolicLink()) throw new Error(`Symbolic links are unsupported: ${fullPath}`);
      if (entry.isDirectory()) visit(fullPath, nextRelative);
      else if (entry.isFile()) found.set(nextRelative, fullPath);
      else throw new Error(`Unsupported source entry: ${fullPath}`);
    }
  }
  visit(root, '');
  return found;
}

function hash(file) {
  return createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

if (!fs.existsSync(path.join(source, 'package.json')) || !fs.existsSync(path.join(destination, 'package.json'))) {
  throw new Error('Both source and destination must contain package.json.');
}
if (fs.realpathSync(source) === fs.realpathSync(destination)) {
  throw new Error('Source and destination must be different directories.');
}
if (fs.existsSync(path.join(destination, '.git'))) {
  throw new Error('The destination contains a Git checkout; refusing to overwrite it.');
}
for (const root of [source, destination]) {
  const manifest = JSON.parse(fs.readFileSync(path.join(root, 'package.json'), 'utf8'));
  if (manifest.name !== 'com.code-philosophy.zlua') {
    throw new Error(`Unexpected Unity package at ${root}: ${manifest.name}`);
  }
}
if (git('status', '--porcelain', '--untracked-files=all')) {
  throw new Error('The ZLua source checkout must be clean.');
}
const revision = git('rev-parse', 'HEAD');
const upstream = files(source);
const local = files(destination);
const changed = [...upstream].filter(([name, file]) => !local.has(name) || hash(file) !== hash(local.get(name)));
const extra = [...local.keys()].filter(name => !upstream.has(name));

if (check) {
  console.log(`ZLua ${revision}: ${upstream.size} source files, ${changed.length} changed or missing, ${extra.length} extra.`);
  if (changed.length || extra.length) process.exitCode = 1;
} else {
  for (const [name, file] of changed) {
    const target = path.join(destination, name);
    fs.mkdirSync(path.dirname(target), { recursive: true });
    fs.copyFileSync(file, target);
  }
  for (const name of extra) fs.unlinkSync(local.get(name));
  function prune(directory) {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      if (entry.isDirectory()) prune(path.join(directory, entry.name));
    }
    if (directory !== destination && fs.readdirSync(directory).length === 0) fs.rmdirSync(directory);
  }
  prune(destination);
  const metadata = [
    '# ZLua upstream',
    '',
    'Source: https://github.com/focus-creative-games/zlua',
    `Revision: ${revision}`,
    '',
    'Update the checkout, then run `node Tools/update-zlua.mjs --source <checkout>`.',
    ''
  ].join('\n');
  fs.writeFileSync(path.join(repository, 'docs', 'zlua-upstream.md'), metadata);
  console.log(`Updated ZLua from ${revision}: copied ${changed.length}, removed ${extra.length}.`);
}

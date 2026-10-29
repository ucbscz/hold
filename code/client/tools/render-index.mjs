import { readFileSync, writeFileSync } from "node:fs";

const source = new URL("../src/", import.meta.url);
const brand = JSON.parse(
  readFileSync(new URL("brand/brand.json", source), "utf8"),
);
const template = readFileSync(new URL("index.template.html", source), "utf8");

const escapeHtml = (value) =>
  value
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");

const resolve = (path) => {
  const value = path.split(".").reduce((node, key) => node?.[key], brand);
  if (typeof value !== "string")
    throw new Error(`brand.json no define "${path}"`);
  return escapeHtml(value);
};

writeFileSync(
  new URL("index.html", source),
  template.replace(/\{\{\s*([\w.]+)\s*\}\}/g, (_, path) => resolve(path)),
);

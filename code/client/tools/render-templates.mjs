import { readFileSync, writeFileSync } from "node:fs";

const root = new URL("../", import.meta.url);
const readJson = (path) =>
  JSON.parse(readFileSync(new URL(path, root), "utf8"));

const context = {
  ...readJson("src/brand/brand.json"),
  services: readJson("src/external-services/external-services.json"),
};

const escapeHtml = (value) =>
  value
    .replace(/&/g, "&amp;")
    .replace(/"/g, "&quot;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");

const resolve = (path) => {
  const value = path.split(".").reduce((node, key) => node?.[key], context);
  if (typeof value !== "string")
    throw new Error(`La configuración no define "${path}"`);
  return escapeHtml(value);
};

const render = (template, output) =>
  writeFileSync(
    new URL(output, root),
    readFileSync(new URL(template, root), "utf8").replace(
      /\{\{\s*([\w.]+)\s*\}\}/g,
      (_, path) => resolve(path),
    ),
  );

render("src/index.template.html", "src/index.html");
render("nginx.template.conf", "nginx.conf");

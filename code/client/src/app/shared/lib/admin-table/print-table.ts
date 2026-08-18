export interface PrintableTable {
  title: string;
  headers: string[];
  rows: unknown[][];
}

export function printTable({ title, headers, rows }: PrintableTable): void {
  const frame = document.createElement('iframe');
  frame.setAttribute('aria-hidden', 'true');
  frame.style.position = 'fixed';
  frame.style.right = '100%';
  frame.style.bottom = '0';
  frame.style.width = '1px';
  frame.style.height = '1px';
  frame.style.border = '0';
  document.body.appendChild(frame);

  const printWindow = frame.contentWindow;
  const printDocument = frame.contentDocument;
  if (!printWindow || !printDocument) {
    frame.remove();
    return;
  }

  const head = headers
    .map((header) => `<th>${escapeHtml(header)}</th>`)
    .join('');
  const body = rows
    .map(
      (row) =>
        `<tr>${row.map((value) => `<td>${escapeHtml(value)}</td>`).join('')}</tr>`,
    )
    .join('');

  printDocument.open();
  printDocument.write(`<!doctype html>
    <html lang="es">
      <head>
        <meta charset="utf-8" />
        <title>${escapeHtml(title)}</title>
        <style>
          @page { size: landscape; margin: 12mm; }
          * { box-sizing: border-box; }
          body { margin: 0; color: #172033; font-family: Arial, sans-serif; }
          h1 { margin: 0 0 14px; font-size: 20px; }
          table { width: 100%; border-collapse: collapse; table-layout: auto; }
          th, td { padding: 7px 8px; border: 1px solid #d7dee8; text-align: left; font-size: 10px; }
          th { background: #f5f7fa; font-weight: 700; }
          tr { break-inside: avoid; }
        </style>
      </head>
      <body>
        <h1>${escapeHtml(title)}</h1>
        <table><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table>
      </body>
    </html>`);
  printDocument.close();

  let removed = false;
  const cleanup = () => {
    if (removed) return;
    removed = true;
    frame.remove();
  };

  printWindow.addEventListener('afterprint', cleanup, { once: true });
  window.setTimeout(() => {
    printWindow.focus();
    printWindow.print();
  }, 100);
  window.setTimeout(cleanup, 60_000);
}

function escapeHtml(value: unknown): string {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;');
}

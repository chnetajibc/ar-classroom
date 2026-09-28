#!/usr/bin/env python3
"""Tiny Markdown -> HTML converter (headings, paragraphs, lists, tables, code fences, inline code/bold/links) used to build the PDF documentation."""
import re, sys, html

def inline(t):
    t = html.escape(t, quote=False)
    t = re.sub(r'`([^`]+)`', r'<code>\1</code>', t)
    t = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', t)
    t = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', r'<a href="\2">\1</a>', t)
    return t

def convert(md, title):
    out, i, lines = [], 0, md.split('\n')
    def flush_list(items, ordered):
        tag = 'ol' if ordered else 'ul'
        out.append('<%s>%s</%s>' % (tag, ''.join('<li>%s</li>' % inline(x) for x in items), tag))
    while i < len(lines):
        l = lines[i]
        if l.startswith('```'):
            i += 1; code = []
            while i < len(lines) and not lines[i].startswith('```'): code.append(lines[i]); i += 1
            out.append('<pre><code>%s</code></pre>' % html.escape('\n'.join(code))); i += 1; continue
        if l.startswith('<!--'): out.append(l); i += 1; continue
        m = re.match(r'^(#{1,4}) (.*)', l)
        if m: n = len(m.group(1)); out.append('<h%d>%s</h%d>' % (n, inline(m.group(2)), n)); i += 1; continue
        if l.startswith('|'):
            rows = []
            while i < len(lines) and lines[i].startswith('|'): rows.append(lines[i]); i += 1
            cells = [[c.strip() for c in r.strip().strip('|').split('|')] for r in rows]
            aligns = cells[1] if len(cells) > 1 and set(''.join(cells[1])) <= set('-: ') else None
            body = cells[2:] if aligns else cells[1:]
            t = '<table><thead><tr>%s</tr></thead><tbody>' % ''.join('<th>%s</th>' % inline(c) for c in cells[0])
            for r in body:
                tds = ''
                for j, c in enumerate(r):
                    al = ' class="num"' if aligns and j < len(aligns) and aligns[j].endswith(':') else ''
                    tds += '<td%s>%s</td>' % (al, inline(c))
                t += '<tr>%s</tr>' % tds
            out.append(t + '</tbody></table>'); continue
        if re.match(r'^\s*[-*] ', l):
            items = []
            while i < len(lines) and re.match(r'^\s*[-*] ', lines[i]): items.append(re.sub(r'^\s*[-*] ', '', lines[i])); i += 1
            flush_list(items, False); continue
        if re.match(r'^\d+\. ', l):
            items = []
            while i < len(lines) and re.match(r'^\d+\. ', lines[i]): items.append(re.sub(r'^\d+\. ', '', lines[i])); i += 1
            flush_list(items, True); continue
        if l.strip() == '': i += 1; continue
        para = [l]; i += 1
        while i < len(lines) and lines[i].strip() and not re.match(r'^(#|\||```|\s*[-*] |\d+\. |<!--)', lines[i]): para.append(lines[i]); i += 1
        out.append('<p>%s</p>' % inline(' '.join(para)))
    css = '''body{font:14px/1.55 -apple-system,Segoe UI,Helvetica,Arial,sans-serif;color:#1c2430;max-width:860px;margin:28px auto;padding:0 22px}
h1{font-size:28px;border-bottom:3px solid #2f6fb1;padding-bottom:8px}h2{font-size:20px;margin-top:30px;color:#22508a;border-bottom:1px solid #d6dde6;padding-bottom:4px}h3{font-size:16px}
code{background:#eef2f7;padding:1px 5px;border-radius:3px;font:12.5px Menlo,Consolas,monospace}pre{background:#0f1720;color:#e8eef6;padding:12px 14px;border-radius:6px;overflow:auto}pre code{background:none;color:inherit;padding:0}
table{border-collapse:collapse;width:100%;margin:12px 0;font-size:13px}th,td{border:1px solid #d6dde6;padding:5px 9px;text-align:left}th{background:#eaf1f9}td.num{text-align:right}
a{color:#22508a}li{margin:2px 0}@media print{body{margin:0;max-width:none}h2{page-break-after:avoid}table,pre{page-break-inside:avoid}}'''
    return '<!doctype html><html><head><meta charset="utf-8"><title>%s</title><style>%s</style></head><body>%s</body></html>' % (html.escape(title), css, '\n'.join(out))

if __name__ == '__main__':
    src, dst, title = sys.argv[1], sys.argv[2], sys.argv[3]
    open(dst, 'w').write(convert(open(src).read(), title))

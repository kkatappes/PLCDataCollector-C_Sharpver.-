import fitz  # PyMuPDF

pdf_path = "./sh080931q.pdf"
doc = fitz.open(pdf_path)

for i in range(len(doc)):
    page = doc[i]
    # 解像度を上げたい場合は matrix で拡大（例：2倍）
    zoom = 2.0
    mat = fitz.Matrix(zoom, zoom)
    pix = page.get_pixmap(matrix=mat, alpha=False)  # alpha=True で透過付き
    pix.save(f"./page_{i+1}.png")
doc.close()
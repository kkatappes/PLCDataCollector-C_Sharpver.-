# settings.txtの16進数を10進数に変換するスクリプト

input_file = r"C:\Users\1010821\Desktop\python\andon\conmoni_test\settings.txt"
output_file = r"C:\Users\1010821\Desktop\python\andon\conmoni_test\settings_decimal.txt"

# ファイル読み込み
with open(input_file, 'r') as f:
    content = f.read()

# カンマで分割して16進数値を取得
hex_values = [h.strip() for h in content.replace('\n', '').split(',') if h.strip()]

# 16進数を10進数に変換
dec_values = [str(int(h, 16)) for h in hex_values]

# 結果をファイルに書き込み
with open(output_file, 'w') as f:
    f.write(','.join(dec_values))

print(f"変換完了: {len(dec_values)}個の値を変換しました")
print(f"出力ファイル: {output_file}")

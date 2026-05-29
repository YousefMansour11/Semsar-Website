import json
with open(r'E:\Projectx\backend\Semsar-Backend-main\Semsar-Backend-main\temp_all_units.json', encoding='utf-8-sig') as f:
    d = json.load(f)
for u in d['data']:
    print(f"id={u['id']} title={u['titleEn']} slug={u['slug']}")

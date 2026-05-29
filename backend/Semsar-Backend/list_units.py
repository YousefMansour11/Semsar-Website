import json
with open('E:/Projectx/backend/Semsar-Backend-main/Semsar-Backend-main/temp_all_units.json') as f:
    d = json.load(f)
for u in d['data']:
    print(f'id={u["id"]} title={u["titleEn"]} slug={u["slug"]}')

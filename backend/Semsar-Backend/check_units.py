import json
d = json.load(open(r'E:\Projectx\backend\Semsar-Backend-main\Semsar-Backend-main\temp_units.json'))
for u in d.get('data', []):
    print(f"id={u['id']} title={u['titleEn']} slug={u['slug']} project={u['projectId']}")

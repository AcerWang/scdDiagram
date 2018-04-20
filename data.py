import db
import re

p = '(#\d主变)|(\d#主变)|(\d号主变)'
pattn = re.compile(p)

trans = set()
#result = re.search(p,'2号主变低压侧测控PCS-9705A-D-H2')
#print(result.group())

#select `name` , `desc` from `ied` where `name` like "%PL%" order by `name`;
#select `name` , `desc` from `ied` where `desc` like "%母%" order by `name`;

# 主变
sql = "select `name` , `desc` from `ied` where `desc` like '%主变%' order by `name`"
res = db.select(sql)
if res is not None:
    for row in res:
        t = pattn.search(row[1]).group()
        t = t.replace("#","")
        trans.add(t)

print("主变台数：",len(sorted(trans,reverse=False)))
for t in trans:
    print(t)
import db
import re

sql = """select LN.id,IEDTree.IED,ExtRef.iedName,ExtRef.lnClass,ExtRef.lnInst,ExtRef.ldInst from (LN,ExtRef) 
INNER JOIN IEDTree on LN.ldevice_id=IEDTree.LD_id 
where LN.ldevice_id in ( SELECT IEDTree.LD_id FROM IEDTree  WHERE  IEDTree.IED LIKE 'M%L%' AND IEDTree.AccessPoint LIKE 'M%' ) 
and LN.lnClass='LLN0' and LN.id=ExtRef.ln0_id and ExtRef.lnClass!='LLN0'"""

def which():
    dbObj = db.DataBase()
    
    res = dbObj.select(sql)
    line = set()
    p = re.compile(r'\d+')
    for i in res:
        line.add(p.search(i[1]).group())
    print(line)

    dbObj.close_connection()

if __name__=='__main__':
    which()
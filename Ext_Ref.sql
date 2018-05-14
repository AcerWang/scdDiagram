-- SELECT ied, AccessPoint, LD_id from IEDTree where ied like 'M%L%' and AccessPoint like 'M%';
-- select ExtRef.* from LN  INNER JOIN ExtRef on LN.id = ExtRef.ln0_id where LN.ldevice_id=38 limit 50 OFFSET 1;
-- select LN.desc,LN.id from LN where LN.ldevice_id=38 and LN.lnClass='TVTR' and LN.inst<=6;

select * from [ExtRef] where [ln0_id] in(
    select id from [LN] where [ldevice_id] in ( SELECT 
         [LD_id]
    FROM   [IEDTree]
    WHERE  [ied] LIKE 'M%L%' AND [AccessPoint] LIKE 'M%' )
  and lnClass='LLN0' )

select * from [ExtRef] where [ln0_id] in(
    select id from [LN] where [ldevice_id] in ( SELECT 
         [LD_id]
    FROM   [IEDTree]
    WHERE  [ied] LIKE 'M%L%' AND [AccessPoint] LIKE 'M%' )
  and lnClass='LLN0' ) and lnClass!='LLN0';

select LN.id,IEDTree.IED from LN INNER JOIN IEDTree on LN.ldevice_id=IEDTree.LD_id where LN.ldevice_id in ( 
SELECT IEDTree.LD_id FROM IEDTree  WHERE  IEDTree.IED LIKE 'M%L%' AND IEDTree.AccessPoint LIKE 'M%' ) and LN.lnClass='LLN0'

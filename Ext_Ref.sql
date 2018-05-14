select LN.id,IEDTree.IED,ExtRef.iedName,ExtRef.lnClass,ExtRef.lnInst,ExtRef.ldInst from (LN,ExtRef) INNER JOIN IEDTree on LN.ldevice_id=IEDTree.LD_id where LN.ldevice_id in ( 
SELECT IEDTree.LD_id FROM IEDTree  WHERE  IEDTree.IED LIKE 'M%L%' AND IEDTree.AccessPoint LIKE 'M%' ) and LN.lnClass='LLN0' and LN.id=ExtRef.ln0_id and ExtRef.lnClass!='LLN0';

select LN.* from LN INNER JOIN IEDTree on LN.ldevice_id=IEDTree.LD_id where LN.lnClass='TVTR' and IEDTree.IED ='MM1101A'  and IEDTree.LDevice ='MUSV01' and LN.inst in (1)

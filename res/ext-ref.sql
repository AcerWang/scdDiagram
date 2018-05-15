-- CREATE TABLE tmp as SELECT IEDTree.IED as Line,ExtRef.iedName as Ref_To,ExtRef.lnClass,ExtRef.lnInst,ExtRef.ldInst,ExtRef.prefix ,ExtRef.doName from LN,ExtRef
-- INNER JOIN IEDTree on LN.ldevice_id=IEDTree.LD_id 
-- where LN.ldevice_id in ( SELECT IEDTree.LD_id FROM IEDTree  WHERE  IEDTree.IED LIKE 'M%L%' AND IEDTree.AccessPoint LIKE 'M%' )
-- and LN.lnClass='LLN0'	and LN.id=ExtRef.ln0_id and ExtRef.lnClass!='LLN0';
 
select DISTINCT tmp.Line,tmp.Ref_To,LN.desc from tmp INNER JOIN LN on LN.inst=tmp.lnInst and LN.lnClass=tmp.lnClass 
where LN.ldevice_id = (select IEDTree.LD_id FROM IEDTree where IEDTree.IED=tmp.Ref_To and IEDTree.LDevice=tmp.ldInst);

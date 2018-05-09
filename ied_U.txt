-- select ied from IED where name = 'ML1104';
-- select * from AccessPoint where ied='IED_97' and name like 'M%';
-- select ldevice from LDevice where accesspoint = 'AccessPoint_229';
-- select ln from LN where ldevice = 'LDevice_387' and ln like 'LN0%';
select * from ExtRef where ln0 = 'LN0_9430';
-- select * from DOI where ln = 'LN0_9430';
-- select * from AccessPoint where ied='IED_105' and name like 'M%';
-- select * from LDevice WHERE inst='MUSV01' and accesspoint='AccessPoint_259';
select * from LN where ldevice = 'LDevice_417' and inst='3' and lnClass='TVTR' and prefix like 'U%';

-- select * from IED where name = 'ML2206A' or name='ML2206B';
-- select * from AccessPoint where ied='IED_37' and name like 'M%';
-- select ldevice from LDevice where accesspoint = 'AccessPoint_85';
-- select ln from LN where ldevice = 'LDevice_128' and ln like 'LN0%';
select * from ExtRef where ln0 = 'LN0_3694';
-- select * from ied where name ='MM2201B';
-- select * from AccessPoint where ied='IED_48' and name like 'M%';
-- select * from LDevice WHERE inst='MUSV' and accesspoint='AccessPoint_106';
select * from LN where ldevice = 'LDevice_156' and inst='4' and lnClass='TVTR';

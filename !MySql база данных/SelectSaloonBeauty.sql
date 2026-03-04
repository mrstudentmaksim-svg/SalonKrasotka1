-- выборка из таблицы Мастера
use saloonBeauty;
select * from masters;

-- выборка из таблицы Клиенты
use saloonBeauty;
select * from clients;

-- выборка из таблицы Услуги
use saloonBeauty;
select * from services;

-- выборка из таблицы ТипыУслуг
use saloonBeauty;
select * from servTypes;

-- выборка из таблицы ОказаниеУслуг
use saloonBeauty;
select * from appointments;

-- выборка из 2-х таблиц
use saloonBeauty;
select masterName, servType
from
masters inner join servTypes on masters.servTypeCode = servTypes.servTypeCode;

-- выборка из 4-х таблиц
use saloonBeauty;
select appDate, masterName, servName, clientName
from
appointments
inner join masters on appointments.masterCode = masters.masterCode
inner join services on appointments.servCode = services.servCode
inner join clients on appointments.clientCode = clients.clientCode;

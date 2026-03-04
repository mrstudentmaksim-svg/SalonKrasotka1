 create database if not exists saloonBeauty ;

-- описываем структуру создаваемых таблиц

use saloonBeauty;

create table if not exists servTypes
(
 servTypeCode int primary key,
 servType varchar (15)
);

 create table if not exists services
  (
   servCode int primary key,
   servName varchar(20),
   servPrice int,
   servDuration int,
   servTypeCode int,
   foreign key(servTypeCode) references servTypes (servTypeCode)
   );
create table if not exists masters
(
 masterCode int primary key,
 masterName varchar(20),
 masterTel varchar(10),
 servTypeCode int,
 foreign key (servTypeCode) references servTypes (servTypeCode)
 );
 create table clients
 (
  clientCode int primary key,
  clientName varchar(20),
  clientTel varchar(10)
  );
   drop table  if exists appointments;
   create table if not exists appointments
   (
    appCode int primary key,
    masterCode int,
    clientCode int,
    servTypeCode int,
    servCode int,
    queueFrom int,
    queueTo int,
    appDate date,
    foreign key (servTypeCode) references servTypes (servTypeCode),
    foreign key (masterCode) references masters (masterCode),
    foreign key (clientCode) references clients (clientCode),
    foreign key (servCode)   references services (servCode)
	);
    -- Добавлая столбца с таблицы Клиенты
    use saloonBeauty;
    alter table clients add clientsActivity varchar(5);
    
    -- Добавлая столбца с таблицы Мастера
    use saloonBeauty;
    alter table masters add mastersActivity varchar(5);
    
    -- Добавлая столбца с таблицы Услуги
    use saloonBeauty;
    alter table services add servicesActivity varchar(5);
    
    -- Добавлая столбца с таблицы ТипыУслуг
    use saloonBeauty;
    alter table servTypes add servTypesActivity varchar(5);
    
    -- Добавлая столбца с таблицы ОказаниеУслуг
    use saloonBeauty;
    alter table appointments add appointmentsActivity varchar(5);
    
    
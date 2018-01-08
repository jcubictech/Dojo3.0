delete from [DojoDev].[dbo].[Lookup] where [Type] = 'Vertical' and [Name] = 'O/O'
delete from [DojoDev].[dbo].[Lookup] where [Type] = 'Vertical' and [Name] = 'O/O+RN'
delete from [DojoDev].[dbo].[Lookup] where [Type] = 'Vertical' and [Name] = 'RM'
delete from [DojoDev].[dbo].[Lookup] where [Type] = 'Vertical' and [Name] = 'RN'
delete from [DojoDev].[dbo].[Lookup] where [Type] = 'Vertical' and [Name] = 'N/A'
update [DojoDev].[dbo].[Lookup] set [Name] = 'HO' where [Type] = 'Vertical' and [Name] = 'Hotels'
update [DojoDev].[dbo].[Lookup] set [Name] = 'CO' where [Type] = 'Vertical' and [Name] = 'SenStay'
insert into [DojoDev].[dbo].[Lookup] ([Type], [Name]) values ('PropertyStatus', 'Pending-Contract')
insert into [DojoDev].[dbo].[Lookup] ([Type], [Name]) values ('PropertyStatus', 'Dead')

update [DojoDev].[dbo].[CPL] set [Vertical] = 'CO' where [Vertical] = 'SenStay'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'HO' where [Vertical] = 'Hotels'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = 'O/O'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = 'O/O+RN'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = 'RM'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = 'RN'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] is null
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = ''
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = 'CHSS'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = 'CHSS or Core'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'FS' where [Vertical] = 'N/A'
update [DojoDev].[dbo].[CPL] set [Vertical] = 'HO' where [Vertical] = 'Payne'

update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = 'O/O'
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = 'O/O+RN'
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = 'RM'
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = 'RN'
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = 'CHSS'
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = 'CHSS or Core'
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = ''
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] = 'Dev Test Accounts'
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'FS' where [Vertical] is null
update [DojoDev].[dbo].[AirbnbAccount] set [Vertical] = 'CO' where [Vertical] = 'SenStay'

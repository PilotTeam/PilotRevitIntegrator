# PilotRevitIntegrator
Комплект из трёх компонентов для обеспечения совместный работы Pilot-ICE и Revit

## Схема взаимодействия компонентов
![Схема](https://github.com/PilotTeam/PilotRevitIntegrator/blob/master/scheme.png)
## 1. Загрузка актуальных версий компонентов
1. Готовые модули для установки можно загрузить по ссылке https://github.com/PilotTeam/PilotRevitIntegrator/releases/tag/v0.1-beta
zip-архив содержит три компонента работающих совместно:
1. PilotRevitAgregator — клиентский модуль расширения Pilot-ICE
1. PilotRevitAddin — Add-In для Revit (поддерживаемая версия 2016 и выше)
1. PilotRevitShareListener — служба Windows для отслеживания изменений RVT в папке revitshare и синхронизации изменений с базой Pilot
## 2. Установка в базу Pilot-ICE модуля PilotRevitAgregator
1. Pilot-ICE → Настройки → Расширения. Добавить файл Ascon.Pilot.SDK.PilotRevitAgregator.zip Настроить права доступа для всех пользователей Revit.
1. Настроить путь к сетевой папке проектов \\server\revitshare (Pilot-ICE → Настройки → Управление общими настройками)
## 3. Установка Add-In для Revit
1. Скопирвоать папку PilotRevitAddin в %ProgramData%\Autodesk\Revit\Addins на всех рабочих местах Revit
Поддерживаются версии Revit 2016 и выше
## 4. Установка службы PilotRevitShareListener на сервер
1. С помощью Pilot-myAdmin создайте в базе данных служебную учётную запись RevitShareListenerUser и назначте на должность RevitShareListenerPosition. Наименование служебной учётной записи и должности могут быть любыми. Учётная запись должна быть либо с правами администратора, либо иметь доступ на создание в папках проектов 
1. Скопируйте папку PilotRevitShareListener в %ProgramData%\
1. В файле settings.xml настроить:
 1. Адрес подключения к серверу `<ServerUrl>http://localhost:5545</ServerUrl>`
 1. Имя вашей базы данных `<DbName>DATABASE_NAME</DbName>`
 1. Логин и пароль служебной учётной записи `<Login>RevitShareListenerUser</Login>` и пароль `<Password>PASSWORD</Password>`
 1. Путь к сетевой папке проектов `<SharePath>\\server\revitshare</SharePath>`. Путь может быть локальным, если PilotRevitShareListener запущен на той же системе где расположена папка \\server\revitshare 
1. Установить 

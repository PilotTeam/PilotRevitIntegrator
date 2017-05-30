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
1. Настроить путь к сетевой папке проектов \\server\revitshare. В меню СЕРВИС Pilot-ICE вызвать Настройки → Управление общими настройками → Revit project path for Agregator
1. Настроить соответсвие сихнхронизируемых атрибутов проектов Pilot-ICE и проектов Revit. В меню СЕРВИС Pilot-ICE вызвать Настройки → Управление общими настройками → Revit project info attributes. Описание конфигурации:
```
<settings>
   <setting pilot="code" revit="Номер проекта"/>
   <setting pilot="project_adress" revit="Адрес проекта"/>
   <setting pilot="project_name" revit="Наименование проекта"/>
   <setting pilot="project_name" revit="Наименование объекта"/>
</settings>
```
## 3. Установка Add-In для Revit
Поддерживаются версии Revit 2016 и выше
1. Скопирвоать содержимое папки PilotRevitAddin в %ProgramData%\Autodesk\Revit\Addins на всех рабочих местах Revit
В результате, при запуске Revit появится вкладка "Pilot-ICE".
## 4. Установка службы PilotRevitShareListener на сервер
1. С помощью Pilot-myAdmin создайте в базе данных служебную учётную запись RevitShareListenerUser и назначте на должность RevitShareListenerPosition. Наименование служебной учётной записи и должности могут быть любыми. Учётная запись должна быть либо с правами администратора, либо иметь доступ на создание в папках проектов 
1. Скопируйте папку PilotRevitShareListener в %ProgramData%\
1. В файле settings.xml настроить:
   * Адрес подключения к серверу `<ServerUrl>http://localhost:5545</ServerUrl>`
   * Имя вашей базы данных `<DbName>DATABASE_NAME</DbName>`
   * Логин и пароль служебной учётной записи `<Login>RevitShareListenerUser</Login>` и пароль `<Password>PASSWORD</Password>`
   * Путь к сетевой папке проектов `<SharePath>\\server\revitshare</SharePath>`. Путь может быть локальным, если PilotRevitShareListener запущен на той же системе где расположена папка \\server\revitshare 
1. Для установки и запуска службы выполните %ProgramData%\PilotRevitShareListener\install.cmd от администратора. Лог в процессе работы записывается в файл listener.log

Все компоненты настроены.

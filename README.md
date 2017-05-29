# PilotRevitIntegrator
Комплект из трёх компонентов для обеспечения совместный работы Pilot-ICE и Revit

## Схема взаимодействия компонентов
![Схема](https://github.com/PilotTeam/PilotRevitIntegrator/blob/master/scheme.png)
## 1. Загрузка актуальных версий компонентов
1. Готовые модули для установки можно загрузить по ссылке https:\\github.com\ 
zip-архив содержит три компонента работающих совместно:
   1. PilotRevitAgrigator — клиентский модуль расширения Pilot-ICE
   1. PilotRevitAddin — Add-In для Revit (поддерживаемая версия 2016 и выше)
   1. 3. PilotRevitShareListener — служба Windows для отслеживания изменений RVT в папке revitshare и синхронизации изменений с базой Pilot
## 1. Установка в базу Pilot-ICE модуля PilotRevitAgrigator
1. Pilot-ICE → Настройки → Расширения. Добавить файл PilotRevitIntegrator.zip Настроить права доступа для всех пользователей Revit.
1. Настроить путь к сетевой папке проектов \\revitshare (Pilot-ICE → Настройки → Управление общими настройками)
## 1. Установка AddIn для Revit (поддерживается версия 2016 и выше)
1. Скопирвоать папку PilotToolbar в C:\ProgramData\Autodesk\Revit\Addins на всех рабочих местах Revit
## 1. Установка службы PilotRevitShareListener на сервер
1. Скопировать файл настроек settings.xml в папку AppData
2. В файле XML настроить параметры подключения, логин, пароль.

# PilotRevitIntegrator
Комплект из трёх компонентов для обеспечения совместный работы Pilot-ICE и Revit.

## Схема взаимодействия компонентов
![Схема](https://github.com/PilotTeam/PilotRevitIntegrator/blob/master/scheme.png)
## 1. Загрузка актуальных версий компонентов
1. Готовые модули для установки можно загрузить [по ссылке](https://github.com/PilotTeam/PilotRevitIntegrator/releases/).
Проверьте отсутствие блокировки на загруженном zip-архиве (Свойства → Разблокировать). Zip-архив содержит три компонента работающих совместно:
1. PilotRevitAgregator — клиентский модуль расширения Pilot-ICE
1. PilotRevitAddin — Add-In для Revit (поддерживаемая версия 2018 и выше)
1. PilotRevitShareListener — служба Windows для отслеживания изменений RVT в папке revitshare и синхронизации изменений с базой Pilot
## 2. Установка в базу Pilot-ICE модуля PilotRevitAgregator
1. Pilot-ICE → Настройки → Расширения. Добавить файл Ascon.Pilot.SDK.PilotRevitAgregator.zip Настроить права доступа для всех пользователей Revit.
1. Настроить путь к сетевой папке проектов `\\server\revitshare`. В меню СЕРВИС Pilot-ICE вызвать Настройки → Управление общими настройками → Revit project path for Agregator
1. Настроить соответствие синхронизируемых атрибутов проектов Pilot-ICE и проектов Revit. В меню СЕРВИС Pilot-ICE вызвать Настройки → Управление общими настройками → Revit project info attributes. Описание конфигурации:
```
<settings>
   <setting pilot="code" revit="Номер проекта"/>
   <setting pilot="project_adress" revit="Адрес проекта"/>
   <setting pilot="project_name" revit="Наименование объекта"/>
</settings>
```
## 3. Установка Add-In для Revit
Поддерживаются версии Revit 2016 и выше. Для Revit 2018 удалите службу Collaboration for Revit [для доступности команды "Рабочие наборы" без предварительного сохранения проекта на диск](https://knowledge.autodesk.com/ru/support/revit-products/learn-explore/caas/CloudHelp/cloudhelp/2018/RUS/Revit-Collaborate/files/GUID-61C821EE-970C-46CC-B3BF-D03BE88E4288-htm.html) 
1. Скопировать содержимое папки PilotRevitAddin в %ProgramData%\Autodesk\Revit\Addins\[папка с номером года] на всех рабочих местах Revit
В результате, при запуске Revit появится вкладка "Pilot-ICE".
## 4. Установка службы PilotRevitShareListener на сервер
1. С помощью Pilot-myAdmin создайте в базе данных служебную учётную запись RevitShareListenerUser и назначте на должность RevitShareListenerPosition. Наименование служебной учётной записи и должности могут быть любыми. Учётная запись должна быть либо с правами администратора, либо иметь доступ на создание в папках проектов 
1. Установите службу, запустив Pilot-RvtShareListener_x64
1. Запустите командную строку, введя `cmd` в Пуске. В командной строке введите `RslAdmin`, чтобы получить список доступных команд управления службой. 
   * Командой `RslAdmin --setPath [путь]` укажите путь к сетевой папке проектов, в которую клиенты Revit будут публиковать изменения моделей. Для службы PilotRevitShareListener этот путь может быть локальным `c:\revitshare` или сетвым  `\\server\revitshare`. При указании сетвого пути убедитесь, что учётная запись, от которой запущена служба PilotRevitShareListener имеет доступ на чтение\запись к указанной сетевой папке.
   * Чтобы изменить частоту обновления проверки сетевой папки, нужно ввести команду RslAdmin `--setDelay [интервал]` (время в миллисекундах)	
   * Измените тип лицензии командой RslAdmin `--setLicenseCode [N]` (например, значение 100 для Pilot-ICE или 103 -для Pilot-ICE Enterprise)
   * Подключитесь к серверу с помощью команды `RslAdmin --connect [url]` , где url - адрес подключения к серверу + имя вашей базы данных (например, `http://localhost:5545/DATABASE_NAME`). Затем введите логин `RevitShareListenerUser` и пароль служебной учётной записи
   * Лог в процессе работы записывается в файл `\\ProgramData\Pilot-RvtShareListener\Logs\listener.log`

Все компоненты настроены.

Внимание! Служба PilotRevitShareListener использует контракт взаимодействия с Pilot-Server, который может изменяться в будущих версиях Pilot-Server. Изменение контракта может привести к неработоспособности службы PilotRevitShareListener. В этом случае обновите службу PilotRevitShareListener до актуальной версии.   


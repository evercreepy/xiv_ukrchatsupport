[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner-direct-single.svg)](https://stand-with-ukraine.pp.ua)

# `G4E Ukrainian Chat Support Plugin`

### === УКРАЇНСЬКОЮ ===

#### UPD від evercreepy: Це форк плагіну G4EUkrChatSupport за авторством justscribe, який нажаль більше не підтримується. Дякую автору за розробку.
Плагін мігровано на Dalamud.NET.Sdk, зроблено незначні виправлення потрібні для підтримки поточної версії Dalamud.

Dalamud плагін для коректного (наскільки дозволяє гра) відображення українських символів у вікні чату гри.  
Суть роботи проста, він автоматично підміняє символи української мови на ті, які відображатимуться не як символ "=".  


#### **Основні деталі**

* Основний функціонал
  * Заміна специфічних українських символів в чаті гри для літер "і", "ї", "є" ("ґ" - не підтримується).
  * Заміну можна налаштувати окремо для вводу, щоб реагувати тільки на українську розкладку (стандартні налаштування мають підійти для 99% людей).
  * Навіть якщо вам в чат написав хтось без плагіна, відобразиться все з заміною.

* Опції
  * Реагувати тільки на українську розкладку (для вікна чату - по замовчуванню `FALSE`).
  * Реагувати тільки на українську розкладку (для вводу - по замовчуванню `TRUE`).
  * Замінювати ввід з клавіатури (по замовчуванню `TRUE`).

#### **Як користуватись**

###### Є два способи:
1. <del> Встановити зі списку плагінів - [детальна інструкція](https://kutok.io/g4eukrchatsupport/yak_vstanovyty_plahin_-hbi). </del> працює наразі тільки з оригінальним плагіном, форка немає в офіційній колекції.

2. Використайте лінк на вкладці "Experimental" в Dalamud - https://raw.githubusercontent.com/evercreepy/xiv_ukrchatsupport/master/repo.json для отримання самих останніх релізів дуже швидко.

### === IN ENGLISH ===
#### UPD from evercreepy: This is a fork of the original G4E plugin created by justscribe, which is unfortunately no longer supported.
Project migrated to Dalamud.NET.Sdk for current Dalamud version support.

A Dalamud plugin to correctly (as far as the game allows) show ukrainian symbols in game chat window.

#### **Main Points**

* Functionality
  * Replace ukrainian specific characters in game chat for "і", "ї", "є" ("ґ" - is not supported).
  * Replace can be configured separately for input to react only to ukrainian layout (default configuration should fit fot 99% of the people).
  * Even if someone wrote in your chat without a plugin, all will be shown with replacement.

* Config options
  * React only to ukrainian layout (for chat window - default is `FALSE`).
  * React only to ukrainian layout (for input - default is `TRUE`).
  * Replace keyboard input (default is `TRUE`).

#### **To Use**

###### There are 2 ways:
1. <del>Install from the list of test plugins. </del> - doesn't work for the plugin fork.
2. Use the link on Dalamud "Experimental" tab - https://raw.githubusercontent.com/evercreepy/xiv_ukrchatsupport/master/repo.json to receive latest releases faster.

# TestTask2D

Тестовое задание: 2D-сцена с персонажем, который ходит в 5 направлениях, собирает книги и может поговорить с камнем.

Unity 6.6 (6000.6.0f1), шаблон 2D (URP), C#.

## Как запустить

Самый простой вариант: открыть `Build/TestTask2D.exe`. Если Windows ругается на неизвестное приложение, нажать «Подробнее» → «Выполнить в любом случае».

Через редактор: добавить папку проекта в Unity Hub, открыть сцену `Assets/Scenes/Main.unity` и нажать Play. Важно открыть именно Main, SampleScene осталась от шаблона и там пусто.

Управление:
- WASD или стрелки - ходьба
- E - взаимодействие (рядом с камнем)
- Esc - выход

На русской раскладке тоже работает, клавиши привязаны по физическому положению.

## Что где лежит

```
Assets/Scripts/
  Input/          PlayerInputReader - чтение клавиатуры
  Movement/       PlayerMovement - движение через Rigidbody2D
  Animation/      FacingDirection, CharacterSpriteAnimator, CharacterSpriteSet, Facing
  Sorting/        YSortRenderer - сортировка по Y
  CameraControl/  CameraFollow, LevelBounds
  Interaction/    Collectible, CollectibleCounter, Interactable, PlayerInteractor
  UI/             CounterView, MessageView, FloatingPrompt
  App/            QuitOnEscape
Assets/Editor/ProjectBuilder.cs - импорт арта, нарезка листа, сборка сцены и билда
ArtSource/       исходный спрайт-лист (с белым фоном, как был)
Build/           готовый билд под Windows
```


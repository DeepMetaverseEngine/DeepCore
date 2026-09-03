# ==GameEditor Schemas==

这个文档定义了整个游戏编辑器的数据定义和目录结构。
其中包括了一些配置数据和构成一个游戏的所有元素模板。
整个游戏编辑器由数据驱动，数据定义了游戏的元素模板，元素模板决定了游戏的元素实体，元素实体构成了游戏世界。
把这些游戏元素编辑到关卡场景里，就可以运行一个游戏逻辑。


编辑器所有数据存储为XML格式，通常来说编辑器目录结构为：
```
GameEditor/
│
├── bin/                            # ✅ 编辑器Exe运行程序可所需要的库。
│
├── data/                           # ✅ 编辑器所有数据
│   ├── aura/*.xml                  # 所有光环模板，.xml文件（AuraTemplate）。
│   ├── buffs/*.xml                 # 所有BUFF模板，.xml文件（BuffTemplate）。
│   ├── cards/*.xml                 # 所有卡牌词缀模板，.xml文件（CardTemplate）。
│   ├── guis/*.xml                  # 所有战斗UI模板，.xml文件（BattleUITemplate）。
│   ├── items/*.xml                 # 所有物品模板，.xml文件（ItemTemplate）。
│   ├── skills/*.xml                # 所有技能模板，.xml文件（SkillTemplate）。
│   ├── spells/*.xml                # 所有法术模板，.xml文件（SpellTemplate）。
│   ├── unit_events/*.xml           # 所有光环模板，.xml文件（UnitEventTemplate）。
│   ├── units/*.xml                 # 所有单位模板，.xml文件（UnitInfo）。
│   ├── scenes/*.xml                # 所有关卡场景模板，（SceneData）.xml文件。
│   ├── scene_graph.xml             # 场景与场景之间的联通关系（SceneGraphData）。
│   ├── config.xml                  # 默认游戏基础配置（DefaultConfig）
│   ├── config_ext.xml              # 默认自定义扩展配置（DefaultExtCFG）
│   ├── config_global.xml           # 自定义全局配置（GlobalCFG）
│   ├── terrain_definition.xml      # 默认地块定义信息（DefaultTerrainDefinitions）。
│   ├── unit_action_definition.xml  # 默认动作定义信息（DefaultUnitActionDefinitions）。
│   ├── data.bytes.xml              # 所有的数据集合（EditorTemplatesMeta）。
│   └── ver.md5                     # 版本号
│
├── res/                            # ✅ 游戏相关资源，包括3D模型，图片，声音，UI等。
│
├── templates/                      # ✅ 游戏相关表格数据，一般有json或者lua格式。
└── vox/                            # ✅ 存储一些三维地图体素信息。
```

整个编辑器数据结构由此定义：**【类型定义：[EditorTemplatesMeta](ZoneEditor/EditorTemplatesMeta.cs)】**

--------------------------------------------------------------------------------------------------

# 1、配置数据

包括了游戏的默认配置，和一些全局配置，包括了：
- 默认游戏基础配置（DefaultConfig）**【类型定义：[Config](Template/Config.cs)】**;
- 默认自定义扩展配置（DefaultExtCFG）【类型继承于ICommonConfig】;
- 自定义全局配置（GlobalCFG）【类型继承于IGlobalConfig】;
- 默认地块定义信息（DefaultTerrainDefinitions）**【类型定义：[TerrainDefinitionMap](Misc/TerrainDefinitionMap.cs)】**;
- 默认角色动作映射表（DefaultUnitActionDefinitions）**【类型定义：[UnitActionDefinitionMap](Misc/UnitActionDefinitionMap.cs)】**;

### 1-1、游戏公共配置：（DefaultConfig）
>类型为[Config](Template/Config.cs)，包含了游戏的公共配置数据，和一些全局配置数据。

### 1-2、扩展配置：（DefaultExtCFG）
>根据不同项目的需要，可以在DefaultExtCFG中添加一些自定义的全局配置数据。

### 1-3、全局配置：（GlobalCFG）
>根据不同项目的需要，可以在GlobalCFG中添加一些自定义的全局配置数据。

### 1-4、地块定义信息：（DefaultTerrainDefinitions）
>地块定义信息[TerrainDefinitionMap](Misc/TerrainDefinitionMap.cs)，定义了游戏中地块的类型和属性。
在游戏中，地块是构成地图的基本单位，不同类型的地块有不同的属性和效果。
由体素工具将一个3D场景体素化，把一个地图按照预定的格子尺寸切分成一个个地块。
每个地块对应一个地块定义信息，地块里面
标记了颜色值，不同的颜色值的地块可以在游戏开发中表示不同的功能，比如MMORPG的安全区，
战斗区，以及地图区域间的国界。

### 1-5、角色动作映射表：（DefaultUnitActionDefinitions）
>地块定义信息[UnitActionDefinitionMap](Misc/UnitActionDefinitionMap.cs)，定义了游戏中地块的类型和属性。
在游戏中，地块是构成地图的基本单位，不同类型的地块有不同的属性和效果。
由体素工具将一个3D场景体素化，把一个地图按照预定的格子尺寸切分成一个个地块。
每个地块对应一个地块定义信息，地块里面
标记了颜色值，不同的颜色值的地块可以在游戏开发中表示不同的功能，比如MMORPG的安全区，
战斗区，以及地图区域间的国界。


--------------------------------------------------------------------------------------------------

# 2、游戏元素模板

组成游戏核心要素的模板数据，包括了：

- 单位：（类型定义：[UnitInfo](Template/Template.Unit.cs)）;
- 技能：（类型定义：[SkillTemplate](Template/Template.Skill.cs)）;
- 法术：（类型定义：[SpellTemplate](Template/Template.Spell.cs)）;
- 状态BUFF：（类型定义：[BuffTemplate](Template/Template.Buff.cs)）;
- 光环AURA：（类型定义：[AuraTemplate](Template/Template.Aura.cs)）;
- 物品：（类型定义：[ItemTemplate](Template/Template.Item.cs)）;
- 单位事件：（类型定义：[UnitEventTemplate](Template/Template.UnitEvent.cs)）;
- 卡牌词缀：（类型定义：[CardTemplate](Template/Template.Card.cs)）;
- 战斗UI：（类型定义：[BattleUITemplate](Template/Template.GUI.cs)）;
- 游戏场景：（类型定义：[SceneData](ZoneEditor/SceneData.cs)）;
- 
--------------------------------------------------------------------------------------------------

### 2-1、单位：（[UnitInfo](Template/Template.Unit.cs)）

>单位是一个游戏里最基本的游戏角色，比如玩家，NPC，怪物，BOSS等。
单位本身可以装备物品，装备技能（Skill），释放技能（Skill）。单位也是BUFF状态的承受者。
法术（Spell）也可以以一个单位为目标给予攻击。单位在光环（Aura）内可以有状态加成。
单位在场景中的碰撞或者受击检测以圆柱体（BodySize+BodyHeight）作为攻击判定。

--------------------------------------------------------------------------------------------------

### 2-2、技能：（[SkillTemplate](Template/Template.Skill.cs)）

技能主要由判定区域和动作序列构成。由单位播放特定动作，并在动作过程中进行关键帧触发行为。

#### 2-2-1、动作序列
>单位可以释放技能，单位释放技能时通常会做一个动作序列（ActionQueue），比如说挥刀或者吟唱。
每个动作时长和美术资源里的播放时长一致，动作由Timeline构成，在每个动作（UnitActionData）
播放过程中会执行一系列关键帧（KeyFrames）。
每个动作和这个单位对应的美术资源的动画系统（Animator、Animation、Timeline等）相关联，注意：动画
系统在本框架内不应该有位移行为，所有位移行为应当通过技能配置的关键帧来控制，否则服务端无法进行
位置运算验证和模拟。
关键帧里定义了一些行为： 
>- 发射子弹或者某个范围性的法术（Spell）；
>- 特效表现，由客户端播放粒子特效（Effect）；
>- 近战攻击判定（Attack）；
>- 给自己上一个BUFF（SelfBuff）；
>- 给自己开启一个光环（SelfAura）；
>- 动作位移、用于冲刺或者跳跃之类有位移的动作（Move）；
>- 闪现位移（Blink）；
>- 动作目标状态改变用于控制前摇（ChangeStatus）；
>- 改变技能目标（ChangeTarget）；
>- 召唤其他单位（Summon）；

#### 2-2-2、判定区域
>技能本身也包含判定区域（AttackShape）和释放距离（AttackRange）等参数。技能在播放过程中如果配置
了Attack关键帧，则这些关键帧在判定时，以这些区域进行对目标攻击判定。
>- Round：圆形（BodySize=半径尺寸）
>- Fan：扇形（BodySize=半径尺寸；FanAngle=角度）
>- Strip：胶囊条状（Distance=长度；RectWide=宽度）
>- StripRay：胶囊射线，以原点出去（Distance=长度；RectWide=宽度）
>- StripRayTouchEnd：胶囊射线，接触到最近（Distance=最大长度；RectWide=宽度）
>- RectStrip：方形条状
>- RectStripRay：方形射线（以原点出去）
>- WideStrip：横向胶囊条状
>- LineToTarget：连线类型（单体攻击），比如激光塔（Distance=判定距离）
>- LineToStart：连线类型（单体攻击），比如伸出去的钩子（Distance=判定距离）
>- LineToSender：连线类型（单体攻击），比如伸出去的钩子（Distance=判定距离）
>- Circle：圆环，中间是空的（BodySize=外环半径；BodySize-RectWide=内环半径）


总结下来：就是技能本身是一个单位（UnitInfo）的动画播放器，在单位播放动画期间，可以做各种行为。

--------------------------------------------------------------------------------------------------

### 2-3、法术:（[SpellTemplate](Template/Template.Spell.cs)）

>法术（Spell）在整个游戏内，承载所有飞行道具、范围伤害和角色碰撞相关的攻击判定。
很多游戏里都有类似的概念，比如《魔兽世界》里有寒冰箭、暴风雪、闪电链等。
法术的美术资源通常由粒子构成，可将美术工程导出的AssetBundle和法术资源做一个绑定。
包括游戏中常见的：子弹、跟踪弹、范围伤害、射线、闪电链、AOE、抛物线等。
通过法术的MotionType和Shape来组合出各种各样法术效果。
法术本身主要包含关键帧（KeyFrames）、判定区域（BodyShape）、和运动方式（MType）参数。

#### 2-3-1、法术移动：
>法术可以作为投射物，也可以作为不动范围性伤害/加血区域，也可以绑定在某个父节点（单位、法术）上跟随父节点
一起移动。法术作为投射物飞行时可以有速度和加速度。法术作为固定伤害区域时也可以设置范围逐渐变大。法术可以自转
（RotateSpeedSEC），也可以绑定在父节点围绕父节点进行公转（IsBindingOrbit）。法术可以以抛物线方式发射出去
（MCannonThrow）并且由初速度和发射角度来控制远近。法术可以跟踪一个目标单位，像导弹（Missile）一样实时变轨，
还可以实时寻找目标并进行跟踪（SeekerMissile）。也可以和法术形状配合，配置成连锁法术比如闪电链，子弹弹射。
>
>法术运动类型：
>- Immovability：在原地不动
>- Straight：按直线运动，射出时移动方向就已经确定。
>- StraightPingPong：先按直线运动，命中后返回到发射者。
>- Forward：按朝向运动，朝向过程中可以改变。
>- Backward：向发射者方向移动，回旋镖。
>- Missile：跟随目标，直到击中。
>- AOE：原地向周围扩散。
>- AOE_Binding：向周围扩散，绑定发射者。
>- AOE_BindingTarget：向周围扩散，绑定被攻击者。
>- Binding：绑定发射者。
>- BindingTarget：绑定被攻击者。
>- Cannon：炮弹类，抛物线。
>- SelectTarget：直接在目标生效
>- SelectLauncher：直接在自身生效
>- SeekerMissile：先按直线运动，过程中锁定目标
>- SeekerSelectTarget：锁定并命中目标，和SeekerMissile区别是没有过程，直接命中。
>- Chain：发射者和目标绑定（Distance必须在范围内）
>- Custom：自定义移动方式，用于代码扩展。

#### 2-3-2、法术形状：

>法术的形状决定了在场景中的碰撞检测区域，和技能的判定检测一致。所有的判定高度统一为BodyHeight。
>- Round：圆形（BodySize=半径尺寸）
>- Fan：扇形（BodySize=半径尺寸；FanAngle=角度）
>- Strip：胶囊条状（Distance=长度；RectWide=宽度）
>- StripRay：胶囊射线，以原点出去（Distance=长度；RectWide=宽度）
>- StripRayTouchEnd：胶囊射线，接触到最近（Distance=最大长度；RectWide=宽度）
>- RectStrip：方形条状
>- RectStripRay：方形射线（以原点出去）
>- WideStrip：横向胶囊条状
>- LineToTarget：连线类型（单体攻击），比如激光塔（Distance=判定距离）
>- LineToStart：连线类型（单体攻击），比如伸出去的钩子（Distance=判定距离）
>- LineToSender：连线类型（单体攻击），比如伸出去的钩子（Distance=判定距离）
>- Circle：圆环，中间是空的（BodySize=外环半径；BodySize-RectWide=内环半径）


#### 2-3-3、法术关键帧：
>子弹本身有一个生命周期，类似技能一样可以通过Timeline来实现各种功能、在生命周期内
播放过程中会执行一系列关键帧（KeyFrames），也可以配置成周期性触发关键帧（IntervalHitKeyFrame），
也可以配置成接触后触发一次的关键帧（OnceHitKeyFrame），也可以配置法术本身接触目标后就爆炸触发
关键帧（HitOnExplosionKeyFrame）。
由于法术可以用来发射法术，则可以用此特性实现一些复杂的技能效果。比如发射一枚大火球，
大火球在每隔1000毫秒发射一发小火球。或者在
击中目标后再发射一发火球。
>
>关键帧里定义了一些行为： 
>- 触发新的法术，可以做到法术的嵌套，连贯表现（Spell）；
>- 特效表现，由客户端播放粒子特效（Effect）；
>- 攻击判定（Attack）；
>- 召唤其他单位（Summon）；
>


--------------------------------------------------------------------------------------------------

### 2-4、状态BUFF：（[BuffTemplate](Template/Template.Buff.cs)）
>状态BUFF这个概念源自于《暗黑破坏神2》《魔兽争霸3》《魔兽世界》。
状态BUFF（Spell）在整个游戏内，用在某个单位实体身上的特殊状态。

#### 2-4-1、BUFF关键帧
>BUFF可以有时效性，时间结束后状态自动清除。BUFF也可以由Timeline时间轴来触发各类行为。
可以配置为特定的时间触发关键帧（KeyFrames），也可以配置成周期性触发关键帧（HitKeyFrame）。
BUFF有叠层的概念，每一层BUFF能力值会有偏差。
>
>BUFF关键帧里定义了一些行为： 
>- 触发新的法术，可以做到法术的嵌套，连贯表现（Spell）；
>- 特效表现，由客户端播放粒子特效（Effect）；
>- 攻击判定（Attack）；
>- 使用物品（UseItem）；
>
#### 2-4-2、BUFF能力
>根据游戏类型的不同，BUFF的作用也有所不同。BUFF能力根据不同项目需要，可以代码或者行为树扩展各类功能。
>模板里自带了一些基本的BUFF能力
>- BuffStateChangeAbility（状态能力）：改变单位的能力（霸体、隐身、无敌、免伤、沉默、限制移动、眩晕）。
>- BuffSpeedChangeAbility（速度改变）：改变施法速度、动作速率、移动速率。
>- BuffEffectAbility（特效能力）：客户端显示用，用来表达单位中了BUFF后，在身体上有对应的美术特效表现。
>- BuffOverlayAbility（堆叠能力）：用来设置BUFF的堆叠层数。
>- BuffAvatarChangeAbility（变身能力）：单位如果中了 BUFF，有改变形态的需求，用来替换美术资源。

--------------------------------------------------------------------------------------------------

### 2-5、光环AURA：（类型定义：[AuraTemplate](Template/Template.Aura.cs)）
>光环（AURA）这个概念源自于《暗黑破坏神2》《魔兽争霸3》《魔兽世界》。
光环是一个绑定在单位上的特殊BUFF集合。光环有一个范围（Range），在单位Range范围内所有匹配的目标单位（ExpectTarget）
都共享这个光环的BUFF（BindingBuffs）。光环还可以配置一个美术特效，在客户端显示为踩在脚下的光环。

--------------------------------------------------------------------------------------------------

### 2-6、物品：（类型定义：[ItemTemplate](Template/Template.Item.cs)）
>物品在编辑器里表示战斗中的掉落物品。这个物品更类似于《魔兽争霸3》的物品，即某一场单局战斗里的临时性物品。
可以掉落到场景里，可以被捡取，可以进背包，可以被装备，可以被使用。

--------------------------------------------------------------------------------------------------

### 2-7、单位事件：（类型定义：[UnitEventTemplate](Template/Template.UnitEvent.cs)）
>游戏的行为树逻辑，可以数据驱动的一系列运行逻辑，图灵完备。
事件通过行为树来编辑，通常用于一些场景事件。通过触发器（Trigger）、条件（Condition）、执行器（Executor）
来搭配复杂逻辑。事件内的所有节点，均可以通过C#代码来扩展。
>
>行为树主要构成结构有：
>- 入口函数节点（Trigger）：
>- 条件判断节点（Condition）：
>- 执行逻辑节点（Executor）：
>- 数据节点（Value）：

事件不仅仅存在于单位事件，单位数据（UnitInfo）、技能数据（SkillTemplate）、Buff状态（BuffTemplate）、
卡牌词缀（CardTemplate）、战斗UI（BattleUITemplate）的数据结构里，都包换事件行为树的逻辑。

--------------------------------------------------------------------------------------------------

### 2-8、卡牌词缀：（类型定义：[CardTemplate](Template/Template.Card.cs)）
>词缀系统可以控制编辑器内所有数据字段，将数据字段分组，并由Card等级来控制每一级别对应的数据字段。
举个简单的例子，比如肉鸽类游戏（RogueLike）有获得游戏奖励卡牌的类型（三选一）。那么获得某个卡片后
可以将游戏内的元素进行升级。比如《向僵尸开炮》游戏，可以选择弹道数量+1来实现获得了卡片后，发射的子弹
由1发变为2发。对于该需求，那么在我们的词缀系统里，可以将发射子弹的([LaunchSpell](Misc/LaunchSpell.cs))
Count字段，和某个卡牌的Count字段进行绑定，那么在卡牌的Count字段里可以有多个等级予以对应，比如1级=1，2级=2，3级=3。
那么就可以实现获得这个卡片后，发射子弹的数量递增的逻辑。

--------------------------------------------------------------------------------------------------

### 2-9、战斗UI：（类型定义：[BattleUITemplate](Template/Template.GUI.cs)）;
>一个可以和游戏逻辑交互的最简单的UI系统。此UI系统主要打通UI操作逻辑和游戏逻辑的互操作。比如在一个类RougeLike的游戏里，
主角升级的时候我们需要弹出三选一界面，那么我们可以编辑好一个三选一的UI界面，将三选一卡牌的内容和UI里的控件进行绑定。
当用户点选某个卡牌的时候，我们通过行为树逻辑：当用户点击卡牌控件A，执行主角获得卡片A的逻辑。
战斗UI结合游戏逻辑可以做出丰富的有UI交互的游戏类型。而不需要去专门的去接一套UI系统。

--------------------------------------------------------------------------------------------------

# 3、游戏场景：（类型定义：[SceneData](ZoneEditor/SceneData.cs)）;
>运行游戏的舞台，整个编辑器里最核心的部分，承载游戏的具体场景和玩法。你可以理解为《魔兽争霸3》的地图编辑器。
上面所有的游戏元素，最终都体现在游戏场景里。在游戏场景里我们可以编辑游戏的地形数据，放入角色、怪物、NPC等单位，也
可以放入物品、区域，路点，怪物刷新点，物品刷新点，空气墙，可破坏物等丰富的游戏元素。
场景也自带场景事件的行为树，游戏设计人员可以根据需求设定各种游戏进程和逻辑。
比如：玩家进入场景后，见到一个NPC，和NPC对话后，打开一扇门，门里的房间刷新了一个怪物，杀死怪物后拿到一个物品，将
物品交给NPC后，NPC再继续给你开下个门。。。。。这一系列的游戏逻辑，都可以用纯行为树的方式编辑出来。

--------------------------------------------------------------------------------------------------
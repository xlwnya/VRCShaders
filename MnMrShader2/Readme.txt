【まんまるしぇーだー_ver2】

☆概要☆
まんまるしぇーだーは光源の影響を受けづらいMatcapによる陰に特化したシェーダーです。
初めて「シェーダー」を触る人はこのリドミをそっ閉じして「はじめてのまんまるしぇーだー.png」を読んでみよう！！

～VerUP内容～

*マスクが反転して一般的な仕様になりました

*頭悪そうだった変数の名前を多少はまともな名前に変更（パラメータの互換性がなくなってるの注意）
*光源の色をグレースケール化して受信する機能を追加
*ShadeSH9の下限にリミッターを追加。（一定以下は暗くならなくなる）
*陰にメインテクスチャを混ぜるように計算方法を変更&調整用の変数を追加
*Outlineに正しくshadeSH9が乗算されるようにOutline機能を全面改修。
*マスクが反転して一般的な仕様になりました
*リムライト用MadcapをShadeMatcapでマスクする機能を追加。
*みんな両面表示しか使わない気がしたので切り替え機能をオミット



☆マニュアル☆

LightColor
　Checkを入れると光源の色味をうけるようになります。
　checkを外した場合と光源の色味の影響はうけず、明暗だけを受信します。

ShadeSH9_UnderLimiter
　この数値以下は暗くならならないようになります。
　1にすると光源の影響を全く受けなくなります（Emissionと違いShadowMatcapの影響は受けます）
　例えば、0.01ぐらいにするば以前まで真っ黒になってた場所でもわずかに明るくなります。


Color	
　基本となる色を指定します。「MainTex」に乗算されます。

MainTex
　基本となる色を指定するテクスチャーです。「Color」に乗算されます。
　使用したいモデルのテクスチャーを割り当てます。

NormalTex
　ノーマルマップを割り当てます。



ShadeMatcap
　陰用のMatcapを割り当てます。乗算Matcapとしても使えますがMainTexが若干混ぜられるので注意。

ShadeMatcapColor
　「ShadeMatcap」の色を調整します。「ShadeMatcap」にオーバーレイされます。

Main_tex_MIX
　「ShadeMatcap」に対してのMainTexの混ぜ方を調整します。0に近いほど濃く、1に近いほど薄くなります。
　（陰に対してMainTexをオーバーレイとスクリーンで合成していてそれのバランスを調整してます。）

ShadeMask
　陰影に対するマスクテクスチャーです。グレースケールで指定します。黒い近いほどマスクされ、白に近いほど影が落ちます。



AddMatcapTex
　光沢用のMatcapです。「AddMatcapColor」が乗算されたあと、「BassColor」に加算されます。

AddMatcapColor
　「AddMatcapTex」の色を調整します。「AddMatcapTex」に乗算されます。明暗で強度調整ができます。

AddMatcap_Map
　光沢に対するマスクテクスチャーです。グレースケールで指定します。黒い近いほどマスクされ、白に近いほど光沢が出ます。(乗算でマスクしてるので色味をつけるのにも使えます)



RimLightMatcap
　光沢用のMatcapです。「RimLightMatcapColor」が乗算されたあと、「BassColor」に加算されます。
　主にリムライト効果等を想定しています。こちらにはマスク機能はありません。

RimLightMatcapColor
　「RimLightMatcap」の色を調整します。「RimLightMatcap」に乗算されます。明暗で強度調整もできます。




EmissionPower
　Emissionの強度です。（光源を無視したアンリットのような表示になっていきます

EmissionMap
　「EmissionPower」と乗算されたあとBassColorに加算されます



Outline_Color（～Outlineのみ）
　アウトラインの色を指定します。

MainTex_Mix（～Outlineのみ）
　Outline_Colorの色にMainTexの色を乗算します。

Outline_Wigth（～Outlineのみ）
　アウトラインの幅を指定ます。

Outline_Mask（～Outlineのみ）
　アウトラインに対するマスクテクスチャーです。グレースケールで指定します。黒い近いほどマスクされ、白に近いほどアウトラインがでます。




☆ライセンス☆
MITライセンスです。
商用利用ok、再配布ok、改造okです☆
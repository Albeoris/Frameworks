namespace Albeoris.Games.FF8.Encoding.Tests;

/// <summary>
/// Byte/text fixtures captured from Final Fantasy VIII dialog data, used to verify
/// that <see cref="TextEncoding"/> decodes (and, where noted, re-encodes) them correctly.
/// </summary>
internal static class TestData
{
    // English dialog sample (Balamb Garden Festival bulletin board message).
    public const String EuropeanBase64 =
        @"WGZfbGlxIGRtcCBqbW1pZ2xlIF9yIHJmZ3Egbl9lYzsCRnNyIHJmY3BjIHVtbENy
IGBjIF9sdyBzbmJfcmNxIGRtcCBfIHVmZ2pjOwJYZl9yQ3EgYGNhX3NxYyBNPCBy
ZmMgS19wYmNsIEpjcXJndF9qIGFmX2dwPAJ1Z2pqIGBjIGpjX3RnbGUgS19wYmNs
OwJNIGJnYiBsbXIgcXJwZ3RjIHJtIGBjIF8gV2NjSDwgX2xiAk0gdW1sQ3IgY3Rj
bCBlcF9ic19yYzsCTSBiY3RtcmNiIF9qaiBrdyByZ2tjIHJtIHJmYyBLX3BiY2wC
SmNxcmd0X2o8IGt3IG5fcXFnbWw7OzsBRnNyIGdsIHJmYyBjbGI8IHJmY3BjIHVf
cSBsbSBLX3BiY2wCSmNxcmd0X2o7ICBYZl9yQ3EgQGFfc3FjIGxtIG1sYwJ0bWpz
bHJjY3BjYiBybSBmY2puIG1zcjsCRnNyIE0gZl90YyBtbGp3IGt3cWNqZCBybSBg
al9rYzsCTSByZm1zZWZyIE0gX2ptbGMgYW1zamIgbnNqaiBnciBtZGQ7AU1Damog
YGMgcXJfcHJnbGUgXyBsY3UgamdkYzwCYHNyIE0gcWdsYWNwY2p3IGZtbmMgcW1r
Y21sYyB1Z2pqAnJfaWMgbXRjcCByZmMgS19wYmNsIEpjcXJndF9qIGFta2tncnJj
Yy4CAkttbWIyYHdjIEZfal9rYCBLX3BiY2wuAiAgW2drYGp3IEhtbGxjcAA=";

    public const String EuropeanText =
        "Thanks for looking at this page.{Line}But there won\u2019t be any updates for a while.{Line}" +
        "That\u2019s because I, the Garden Festival chair,{Line}will be leaving Garden.{Line}" +
        "I did not strive to be a SeeD, and{Line}I won\u2019t even graduate.{Line}" +
        "I devoted all my time to the Garden{Line}Festival, my passion...{Next}" +
        "But in the end, there was no Garden{Line}Festival.  That\u2019s \u2018cause no one{Line}" +
        "volunteered to help out.{Line}But I have only myself to blame.{Line}" +
        "I thought I alone could pull it off.{Next}" +
        "I\u2019ll be starting a new life,{Line}but I sincerely hope someone will{Line}" +
        "take over the Garden Festival committee!{Line}{Line}" +
        "Good-bye Balamb Garden!{Line}  Wimbly Donner{End}";

    // Japanese dialog sample (same bulletin board message), decoded using the
    // "bgroom_6" field's extension characters.
    public const String JapaneseBase64 =
        @"c3O7GWKFb62Fi6krh4leAkWlXXObT/82YRuSGYYZ4kV5XgJ9rWEZRBkwGt8ZkBlx
HCAZKhnnmxwhKwIq/0S4uxwiq2unRXleAuAZIBkg0aUaXxtRdTldHCMayxwkGUGl
ApO5m32bXgJjf3mnGUQZMBrfmxmQGtS7Gl8bUXd/Ahwhm1RTGdgbbRo0XY23s6sZ
vhwlGYsZ2F4BG48azF0ZRBkwGt9hRW2Ta71/XgI/vYVdGdulHCYacHeFAm+tk425
P6W5mV4CjbGNsV1UGbJFpZO5h2uTq4cCGo29fxwhKxwna71/m0V5XgFzmxrKG0S7
HCiVXRmvd40ZuhuCK7khqZ15XgIcKbdvIV0Z22srGUQZMBrfGZAZcRwgGSq7AhvK
bRvSjUVvrZ15tYmV6AICIIwgjF0gpqAq/0S46AJfGUQZMBrfGZAZcRwgGSoZ54jK
uCSo//o+kv8AAAA=";

    public const String JapaneseFieldName = "bgroom_6";

    public const String JapaneseText =
        "ここを見てくれてありがとう。{Line}でも、このページは休止中です。{Line}それは学園祭実行委員長の僕が{Line}ガーデンを去るからです。{Line}ＳｅｅＤも目指さず、卒業資格も{Line}なんのその。{Line}ひたすら学園祭の実現を目指した{Line}僕の１０代後半、いわゆる青春時代。{Next}結局、学園祭はできなかった。{Line}だって、誰も協力して{Line}くれないんだもんね。{Line}いやいや、１人でもなんとかなると{Line}思った僕が甘かったのです。{Next}この教訓を胸に、新しい生活がんばります。{Line}願わくば、誰かが学園祭実行委員を{Line}引き継いでくれますように！{Line}{Line}バイバイ、バラムガーデン！{Line}　学園祭実行委員長ウェンブリー・ダナー{End}{End}{End}";

    public const String RussianGameText = "ПPИBET, CKBOЛЛ! ДOБPO ПOЖAЛOBATЬ B ГAPДEH.";

    public const String RussianBase64 = "glR/RklYPCBHT0ZTgYEuIHtTeVRTIIJTfUWBU0ZFWIsgRiB6RVR7SUw7";
}

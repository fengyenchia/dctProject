"""Render this folder's MR functional layout, not a construction drawing."""
from pathlib import Path
from html import escape
import math
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
MODE='MR'
W,H=1920,1200
BG='#f7f9fc'; INK='#24334b'; MUTED='#66758b'
im=Image.new('RGB',(W,H),BG);d=ImageDraw.Draw(im)
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',f'<rect width="{W}" height="{H}" fill="{BG}"/>']

def text(x,y,t,size=24,anchor='start',color=INK):
    f=ImageFont.truetype('C:/Windows/Fonts/msjh.ttc',size)
    d.text((x,y),t,font=f,fill=color,anchor='lm' if anchor=='start' else 'mm')
    svg.append(f'<text x="{x}" y="{y}" dominant-baseline="central" font-family="Microsoft JhengHei, sans-serif" font-size="{size}" fill="{color}" text-anchor="{anchor}">{escape(t)}</text>')

def rect(x,y,w,h,fill,stroke='#8a99ad',r=12,dash=False):
    d.rounded_rectangle((x,y,x+w,y+h),radius=r,fill=None if fill=='none' else fill,outline=stroke,width=3)
    svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" stroke="{stroke}" stroke-width="3"'+(' stroke-dasharray="10 7"' if dash else '')+'/>')

def line(pts,color='#8290a5',dash=False,width=3,arrow=False):
    svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in pts)+f'" fill="none" stroke="{color}" stroke-width="{width}"'+(' stroke-dasharray="10 7"' if dash else '')+'/>')
    for a,b in zip(pts,pts[1:]):
        dx,dy=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dy)
        if not dash:d.line((a,b),fill=color,width=width)
        elif length:
            for at in range(0,int(length),17):
                stop=min(at+10,length)
                d.line((a[0]+dx*at/length,a[1]+dy*at/length,a[0]+dx*stop/length,a[1]+dy*stop/length),fill=color,width=width)
    if arrow:
        a,b=pts[-2:];angle=math.atan2(b[1]-a[1],b[0]-a[0])
        tip=[b,(b[0]-14*math.cos(angle-.4),b[1]-14*math.sin(angle-.4)),(b[0]-14*math.cos(angle+.4),b[1]-14*math.sin(angle+.4))]
        d.polygon(tip,fill=color)
        svg.append('<polygon points="'+' '.join(f'{x:.2f},{y:.2f}' for x,y in tip)+f'" fill="{color}"/>')

def circle(x,y,r,fill,stroke,width=3,dash=False):
    d.ellipse((x-r,y-r,x+r,y+r),fill=None if fill=='none' else fill,outline=stroke,width=width)
    svg.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}" stroke="{stroke}" stroke-width="{width}"'+(' stroke-dasharray="10 7"' if dash else '')+'/>')

def user(x,y,label='Quest 3 觀眾'):
    circle(x,y,27,'#c2e5ff','#4b84b1')
    rect(x-38,y+34,76,45,'#c2e5ff','#4b84b1')
    text(x,y-55,label,22,'middle')

def target(x,y,label):
    rect(x-113,y-45,226,90,'#eee7fb','#9a7ac7',dash=True)
    text(x,y-12,label+' 虛擬群落',21,'middle')
    text(x,y+20,'初始隱藏／循聲尋找',18,'middle','#76569c')

text(65,60,f'音虫 {MODE}｜展場與互動配置',38)
text(65,110,'PC 執行 Unity，Quest 3 透視真實展場；只有回收站有實體外殼，虛擬模型與特效疊合其上。',22,color=MUTED)
rect(90,215,1370,660,'#eef1f5','#596b84')
text(90,175,'實際展場｜11.5 × 5 公尺估算，非按比例施工圖',22,color=MUTED)
text(770,255,'一次一人｜在已設定活動區內實際走動與轉頭',24,'middle',MUTED)
target(405,375,'A01');target(1120,400,'A02');target(460,615,'B01')
user(845,505)
line([(885,550),(1110,570)],'#d29b57',arrow=True)
circle(1110,570,27,'#fff1df','#d29b57')
text(1080,620,'Mask 隨已對位表面落點移動',20,'middle','#9d6d32')
text(1040,665,'城市與音虫留在展場座標',22,'middle')
text(1180,705,'不跟著觀眾轉頭移動',21,'middle',MUTED)
rect(705,710,280,115,'#d9eee7','#459489')
rect(689,694,312,147,'none','#9a7ac7',dash=True)
text(845,748,'回收站｜開場朝向後方',22,'middle')
text(845,788,'實體外殼＋虛擬疊加',20,'middle','#76569c')
line([(1005,765),(1090,780)],'#9a7ac7')
text(1110,780,'走近 → 前端進感應區',18)
text(1110,815,'到站後可移開再確認',18,color=MUTED)
text(180,840,'離場',23)
rect(1510,215,335,660,'#e3e8ef','#8a99ad')
text(1677,260,'活動區外／接待後台',25,'middle')
for y,a,b in [(340,'高效能 Windows PC','Unity＋Gateway＋管理'),(505,'工作人員螢幕 ×1','Unity 監看／管理／除錯'),(690,'遊戲影像走 USB Link','管理／成果走 WebSocket')]:
    rect(1532,y-35,291,120,'#ffffff')
    text(1677,y+5,a,22,'middle');text(1677,y+46,b,19,'middle',MUTED)
text(1510,175,'沒有投影機或幕面',22,color=MUTED)

rect(90,930,505,110,'#ffffff','#7099c9')
text(342,966,'觀眾螢幕 ×1／出口取票',24,'middle')
text(342,1005,'前導影片 → 成果 QR／聲音收藏票',21,'middle',MUTED)
text(650,938,'同一台 PC：雙螢幕延伸顯示、遊戲與成果服務',23)
text(650,976,'出口：USB 感熱印表機，列印每場一張聲音收藏票',21,color=MUTED)
text(650,1012,'Quest 3：畫面／內建喇叭與麥克風／追蹤；控制器：瞄準／UI／震動',21,color=MUTED)
text(65,1090,'光罩隨控制器指向移動；有效命中群落才累積捕捉，移開保留原群進度與外觀。',21,color=MUTED)
text(65,1140,'場地約 11.5 × 5m；回收站位於開場朝向後方。對位、可及範圍與線材導引須實機驗證。',19,color=MUTED)
svg.append('</svg>')
(ROOT/'assets'/f'{MODE}展場配置.svg').write_text('\n'.join(svg)+'\n',encoding='utf-8')
im.save(ROOT/'assets'/f'{MODE}展場配置.png')
print(MODE,'layout rendered',W,H)

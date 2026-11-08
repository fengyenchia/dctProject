"""Render this folder's VR functional layout, not a construction drawing."""
from pathlib import Path
from html import escape
import math
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[1]
MODE='VR'
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
text(65,110,'單一城市；有效區隨機分散最多 3 群，捕捉 1 群即補 1 群，SAN 歸零停止補充。',23,color=MUTED)
rect(65,215,840,660,'#eef1f5','#596b84')
rect(1025,215,830,660,'#f2edfa','#9a7ac7')
text(485,255,'真實展場｜身體活動區',28,'middle')
text(1440,255,'Quest 3 內｜單一虛擬城市',28,'middle','#76569c')
circle(450,550,240,'#fff1df','#d29b57',dash=True)
circle(450,550,180,'#e6f3ef','#459489',width=4)
user(450,530)
text(450,660,'活動圈 Ø3m｜移動 1:1',21,'middle','#377b6f')
text(450,765,'外加 0.5m 淨空｜總占位 Ø4m',20,'middle','#9d6d32')
rect(750,330,125,125,'#ffffff')
text(812,368,'PC 主機',21,'middle')
text(812,410,'位於區外',18,'middle',MUTED)
text(780,545,'工作人員',21,'middle')
text(780,580,'區外看護',18,'middle',MUTED)
text(485,827,'圓圈標示＋系統邊界＋淨空／看護',21,'middle')
line([(922,550),(1005,550)],'#7099c9',arrow=True)
text(963,505,'USB Link',18,'middle')
target(1190,365,'A01');target(1685,410,'A02');target(1210,655,'B01')
user(1440,540,'同場景觀眾視點')
line([(1470,580),(1690,550)],'#d29b57',arrow=True)
circle(1690,550,30,'#fff1df','#d29b57')
text(1645,620,'Mask 隨指向移動',21,'middle','#9d6d32')
rect(1317,715,245,110,'#eee7fb','#9a7ac7')
text(1440,748,'虛擬回收站',23,'middle','#76569c')
text(1440,791,'開場後方 0.65m／可伸手',18,'middle')
text(1440,837,'目標在可指向範圍，回收站在伸手可及範圍',20,'middle','#76569c')
text(485,175,'場地約 11.5 × 5m；尺寸為原型配置',21,'middle',MUTED)
text(1440,175,'所有互動在同一城市場景完成',21,'middle',MUTED)
rect(90,920,240,130,'#ffffff','#7099c9')
text(210,956,'觀眾螢幕 ×1',23,'middle')
text(210,1005,'播放前導影片',19,'middle',MUTED)
rect(355,920,240,130,'#ffffff','#7099c9')
text(475,956,'工作人員螢幕 ×1',22,'middle')
text(475,1005,'Unity 監看／管理／除錯',18,'middle',MUTED)
text(650,938,'同一台 PC：雙螢幕延伸顯示、遊戲與成果服務',23)
text(650,976,'出口：USB 列印聲音收藏票；成果 QR 僅印在紙票上',21,color=MUTED)
text(650,1012,'Quest 3：畫面／內建喇叭與麥克風／追蹤；控制器：瞄準／UI／震動',21,color=MUTED)
text(65,1090,'Mask 隨場景落點移動；捕捉只在有效對準音虫時累積，移開保留進度與外觀。',21,color=MUTED)
text(65,1140,'活動圈 Ø3m＋外圍 0.5m 緩衝；圓心沿場地長邊 4.5m、短邊 2.5m，布展前核對淨空與線材。',19,color=MUTED)
svg.append('</svg>')
(ROOT/'assets'/f'{MODE}展場配置.svg').write_text('\n'.join(svg)+'\n',encoding='utf-8')
im.save(ROOT/'assets'/f'{MODE}展場配置.png')
print(MODE,'layout rendered',W,H)

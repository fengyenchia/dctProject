"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { LayoutGroup, motion, useMotionValueEvent, useReducedMotion, useScroll } from "motion/react";
import { IconMenu2 } from "@tabler/icons-react";
import { Button } from "@/components/ui/button";
import { Sheet, SheetTrigger, SheetContent, SheetClose, SheetTitle } from "@/components/ui/sheet";

const navigation = [
  { href: "/", label: "Home" },
  { href: "/results", label: "Results" },
  { href: "/about", label: "About" },
  { href: "/display", label: "Display" },
];

export default function SiteHeader() {
  const pathname = usePathname();
  return <ScrollHeader pathname={pathname} />;
}

function ScrollHeader({ pathname }: { pathname: string }) {
  const headerRef = useRef<HTMLElement>(null);
  const travel = useRef(0);
  const [visibility, setVisibility] = useState({ pathname, hidden: false, menuOpen: false });
  const { scrollY } = useScroll();
  const reduceMotion = useReducedMotion();
  const menuOpen = visibility.pathname === pathname && visibility.menuOpen;
  const hidden = visibility.pathname === pathname && visibility.hidden && !menuOpen;
  const setHidden = (value: boolean) => setVisibility(previous =>
    previous.pathname === pathname && previous.hidden === value
      ? previous : { pathname, hidden: value, menuOpen: previous.pathname === pathname && previous.menuOpen });
  const setMenuOpen = (open: boolean) => setVisibility({ pathname, hidden: false, menuOpen: open });

  // 保留導覽元件，讓底線能跨頁銜接；換頁時只重設顯示狀態。
  if (visibility.pathname !== pathname) setVisibility({ pathname, hidden: false, menuOpen: false });

  useEffect(() => {
    const desktop = window.matchMedia('(min-width: 64rem)');
    const closeOnDesktop = () => {
      if (desktop.matches) setVisibility(previous => previous.menuOpen ? { ...previous, menuOpen: false } : previous);
    };
    desktop.addEventListener('change', closeOnDesktop);
    return () => desktop.removeEventListener('change', closeOnDesktop);
  }, []);

  useMotionValueEvent(scrollY, "change", (latest) => {
    const current = Math.max(0, latest);
    const delta = current - Math.max(0, scrollY.getPrevious() ?? current);
    if (current <= 8) {
      travel.current = 0;
      setHidden(false);
      return;
    }
    if (!delta || menuOpen || headerRef.current?.querySelector(':focus-visible')) {
      travel.current = 0;
      return;
    }
    travel.current = Math.sign(delta) === Math.sign(travel.current)
      ? travel.current + delta : delta;
    // 往下捲隱藏；往上捲一點顯示，避免小幅抖動反覆切換。
    if (travel.current >= 8) setHidden(true);
    if (travel.current <= -6) setHidden(false);
  });

  return (
    <motion.header
      ref={headerRef}
      className="site-header"
      layoutRoot
      initial={false}
      animate={{ y: hidden ? "-100%" : "0%" }}
      transition={{ duration: reduceMotion ? 0 : 0.24, ease: "easeOut" }}
      style={{ pointerEvents: hidden ? "none" : "auto" }}
      onFocusCapture={() => setHidden(false)}
    >
      <div className="site-header-inner">
        <Link className="site-brand" href="/" aria-label="THE UNHEARD — Home">
          {/* 正式 Logo 圖檔交付後替換此文字標記。 */}
          <span className="brand-wordmark">THE UNHEARD</span>
        </Link>
        <LayoutGroup id="site-navigation">
          <nav
            className="site-navigation hidden lg:block"
            aria-label="Primary navigation"
            lang="en"
          >
            <ul className="site-nav">
              {navigation.map(({ href, label }) => {
                const active =
                  href === "/"
                    ? pathname === "/"
                    : pathname === href || pathname.startsWith(`${href}/`);
                return (
                  <li key={href}>
                    <Link
                      href={href}
                      className="nav-link"
                      aria-current={active ? "page" : undefined}
                    >
                      {label}
                      {active && (
                        <motion.span
                          className="nav-indicator"
                          layoutId="active-underline"
                          initial={false}
                          transition={reduceMotion
                            ? { duration: 0 }
                            : { type: "tween", duration: 0.4, ease: [0.4, 0, 0.2, 1] }}
                          aria-hidden="true"
                        />
                      )}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </nav>
        </LayoutGroup>
        <Sheet open={menuOpen} onOpenChange={setMenuOpen}>
          <SheetTrigger asChild>
            <Button variant="ghost" className="site-mobile-toggle lg:hidden [&_svg]:size-6"
              aria-label="開啟選單">
              <IconMenu2 aria-hidden="true" />
            </Button>
          </SheetTrigger>
          <SheetContent aria-describedby={undefined}>
            <SheetTitle className="sr-only">Navigation</SheetTitle>
            <nav aria-label="Mobile navigation" lang="en">
              <ul className="mobile-sheet-nav">
                {navigation.map(({ href, label }) => {
                  const active = href === '/' ? pathname === '/' : pathname === href || pathname.startsWith(`${href}/`);
                  return <li key={href}><SheetClose asChild>
                    <Link className="mobile-sheet-link" href={href} aria-current={active ? 'page' : undefined}>{label}</Link>
                  </SheetClose></li>;
                })}
              </ul>
            </nav>
          </SheetContent>
        </Sheet>
      </div>
    </motion.header>
  );
}

"use client";

// Adapted from shadcn/ui new-york/sheet (MIT); Motion, Tabler and project tokens.
import * as React from 'react';
import * as SheetPrimitive from '@radix-ui/react-dialog';
import { AnimatePresence, motion, useReducedMotion } from 'motion/react';
import { IconX } from '@tabler/icons-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

const SheetContext = React.createContext(false);

function Sheet({ open, defaultOpen = false, onOpenChange, children, ...props }: React.ComponentProps<typeof SheetPrimitive.Root>) {
  const [internalOpen, setInternalOpen] = React.useState(defaultOpen);
  const visible = open ?? internalOpen;
  return <SheetContext.Provider value={visible}>
    <SheetPrimitive.Root {...props} open={visible} onOpenChange={value => {
      setInternalOpen(value);
      onOpenChange?.(value);
    }}>{children}</SheetPrimitive.Root>
  </SheetContext.Provider>;
}

const SheetTrigger = SheetPrimitive.Trigger;
const SheetClose = SheetPrimitive.Close;
const SheetTitle = SheetPrimitive.Title;

const SheetContent = React.forwardRef<
  React.ElementRef<typeof SheetPrimitive.Content>,
  Omit<React.ComponentPropsWithoutRef<typeof SheetPrimitive.Content>, 'asChild' | 'forceMount'>
>(({ className, children, ...props }, ref) => {
  const open = React.useContext(SheetContext);
  const reduceMotion = useReducedMotion();
  const duration = reduceMotion ? 0 : 0.25;
  return <SheetPrimitive.Portal forceMount>
    <AnimatePresence>
      {open && <SheetPrimitive.Overlay key="overlay" forceMount asChild>
        <motion.div className="sheet-overlay" data-slot="sheet-overlay"
          initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration }} />
      </SheetPrimitive.Overlay>}
      {open && <SheetPrimitive.Content key="content" forceMount asChild ref={ref} {...props}>
        <motion.div className={cn('sheet-content', className)} data-slot="sheet-content"
          initial={{ x: reduceMotion ? 0 : '100%' }} animate={{ x: 0 }} exit={{ x: reduceMotion ? 0 : '100%' }}
          transition={{ duration, ease: [0.4, 0, 0.2, 1] }}>
          {children}
          <SheetClose asChild>
            <Button variant="ghost" className="sheet-close [&_svg]:size-6" aria-label="關閉選單">
              <IconX aria-hidden="true" />
            </Button>
          </SheetClose>
        </motion.div>
      </SheetPrimitive.Content>}
    </AnimatePresence>
  </SheetPrimitive.Portal>;
});
SheetContent.displayName = 'SheetContent';

export { Sheet, SheetTrigger, SheetClose, SheetContent, SheetTitle };

// Adapted from shadcn/ui new-york/card (MIT).
import * as React from 'react';
import { cn } from '@/lib/utils';

const Card = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => <div ref={ref} data-slot="card"
    className={cn('rounded-xl border border-foreground/20 bg-background text-foreground', className)} {...props} />,
);
Card.displayName = 'Card';

export { Card };

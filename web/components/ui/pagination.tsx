// Adapted from shadcn/ui new-york/pagination (MIT); uses Next Link, Tabler and project tokens.
import * as React from 'react';
import Link from 'next/link';
import { IconChevronLeft, IconChevronRight } from '@tabler/icons-react';
import { buttonVariants } from '@/components/ui/button';
import { cn } from '@/lib/utils';

function Pagination({ className, ...props }: React.ComponentProps<'nav'>) {
  return <nav aria-label="成果分頁" data-slot="pagination"
    className={cn('mx-auto flex w-full justify-center pt-4', className)} {...props} />;
}

function PaginationContent({ className, ...props }: React.ComponentProps<'ul'>) {
  return <ul data-slot="pagination-content"
    className={cn('flex flex-row items-center gap-1', className)} {...props} />;
}

function PaginationItem(props: React.ComponentProps<'li'>) {
  return <li data-slot="pagination-item" {...props} />;
}

type PaginationLinkProps = React.ComponentProps<typeof Link> & {
  isActive?: boolean;
  size?: 'default' | 'icon';
};

function PaginationLink({ className, isActive, size = 'icon', ...props }: PaginationLinkProps) {
  return <Link aria-current={isActive ? 'page' : undefined} data-slot="pagination-link"
    className={cn(
      buttonVariants({ variant: isActive ? 'outline' : 'ghost' }),
      'min-h-11 min-w-11 rounded-lg text-sm',
      size === 'icon' ? 'h-11 w-11 px-0 py-0' : 'px-3 py-2',
      className,
    )} {...props} />;
}

function PaginationPrevious({ className, ...props }: React.ComponentProps<typeof PaginationLink>) {
  return <PaginationLink aria-label="上一頁" size="default" className={cn('gap-1', className)} {...props}>
    <IconChevronLeft size={16} aria-hidden="true" />
    <span>Previous</span>
  </PaginationLink>;
}

function PaginationNext({ className, ...props }: React.ComponentProps<typeof PaginationLink>) {
  return <PaginationLink aria-label="下一頁" size="default" className={cn('gap-1', className)} {...props}>
    <span>Next</span>
    <IconChevronRight size={16} aria-hidden="true" />
  </PaginationLink>;
}

export { Pagination, PaginationContent, PaginationItem, PaginationLink, PaginationPrevious, PaginationNext };

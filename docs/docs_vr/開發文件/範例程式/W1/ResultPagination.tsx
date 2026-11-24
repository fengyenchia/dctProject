import {
  Pagination, PaginationContent, PaginationItem,
  PaginationLink, PaginationPrevious, PaginationNext,
} from '@/components/ui/pagination';

export default function ResultPagination({ page, hasMore }: { page: number; hasMore: boolean }) {
  if (page === 1 && !hasMore) return null;

  return <Pagination>
    <PaginationContent>
      {page > 1 && <>
        <PaginationItem><PaginationPrevious href={`/results?page=${page - 1}`} /></PaginationItem>
        <PaginationItem><PaginationLink href={`/results?page=${page - 1}`} aria-label={`第 ${page - 1} 頁`}>{page - 1}</PaginationLink></PaginationItem>
      </>}
      <PaginationItem>
        <PaginationLink href={`/results?page=${page}`} isActive aria-label={`第 ${page} 頁`}>{page}</PaginationLink>
      </PaginationItem>
      {hasMore && <>
        <PaginationItem><PaginationLink href={`/results?page=${page + 1}`} aria-label={`第 ${page + 1} 頁`}>{page + 1}</PaginationLink></PaginationItem>
        <PaginationItem><PaginationNext href={`/results?page=${page + 1}`} /></PaginationItem>
      </>}
    </PaginationContent>
  </Pagination>;
}

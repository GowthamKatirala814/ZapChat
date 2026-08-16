import type { CursorPage } from "../types/api";

/**
 * Cursor-paged history helpers, shared by room chat and direct messages.
 *
 * The server returns each page oldest→newest, and `pages[0]` is the newest block, so
 * chronological order across pages is the reverse of the page array. Getting this wrong
 * is silent — the messages all render, just in the wrong order — so it lives in one
 * place rather than being re-derived per feature.
 */

export interface Paged<T> {
  pages: Array<CursorPage<T>>;
}

/** Flattens the paged cache into one chronological list. */
export function flattenPages<T>(data: Paged<T> | undefined): T[] {
  if (!data) return [];
  return [...data.pages].reverse().flatMap((page) => page.items);
}

/** Appends an item to the newest page, ignoring one already present. */
export function appendToNewestPage<T extends { id: string }>(
  data: Paged<T> | undefined,
  item: T,
): Paged<T> | undefined {
  // Nothing cached means the conversation has not been opened; the next fetch has it.
  if (!data || data.pages.length === 0) return data;

  if (data.pages.some((page) => page.items.some((existing) => existing.id === item.id))) {
    return data;
  }

  const [newest, ...rest] = data.pages;

  return { ...data, pages: [{ ...newest, items: [...newest.items, item] }, ...rest] };
}

/** Replaces one item wherever it sits in the paged cache. */
export function patchInPages<T extends { id: string }>(
  data: Paged<T> | undefined,
  id: string,
  update: (item: T) => T,
): Paged<T> | undefined {
  if (!data) return data;

  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      items: page.items.map((item) => (item.id === id ? update(item) : item)),
    })),
  };
}

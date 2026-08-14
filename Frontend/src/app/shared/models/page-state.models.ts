export interface LoadState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}
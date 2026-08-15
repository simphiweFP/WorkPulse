export interface AppError {
  status: number;
  title: string;
  message: string;
  validationErrors?: Record<string, string[]>;
}

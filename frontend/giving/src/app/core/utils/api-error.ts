export function apiErrorMessage(error: any, fallback: string): string {
  if (!error) return fallback;
  if (error.error?.message) return error.error.message;
  if (error.message) return error.message;
  if (error.status === 0) return 'Network error. Check your internet connection.';
  if (error.status === 404) return 'Page or resource not found.';
  if (error.status === 403) return 'You do not have permission to access this.';
  if (error.status === 401) return 'Your session has expired. Please log in again.';
  if (error.status >= 500) return 'Server error. Please try again later.';
  return fallback;
}

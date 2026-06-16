import { AuthService } from '../services/auth.service';

export function appInitializer(auth: AuthService): () => Promise<void> {
  return async () => {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return;
    try {
      await auth.silentRefresh(refreshToken);
    } catch {
      localStorage.removeItem('refreshToken');
    }
  };
}

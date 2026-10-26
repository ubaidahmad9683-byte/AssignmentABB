import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = sessionStorage.getItem('tictactoe-token');
  return token
    ? next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))
    : next(request);
};

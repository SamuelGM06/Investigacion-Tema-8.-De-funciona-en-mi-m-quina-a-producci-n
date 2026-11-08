import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { AuthResponse, Credentials } from '../models/credentials.model';


const TOKEN_KEY = 'todoapp_token';

@Service()
export class AuthService {

    private readonly tokenSignal = signal<string | null>(localStorage.getItem(TOKEN_KEY));
    readonly isLoggedIn = computed(() => this.tokenSignal() !== null);
    private readonly http = inject(HttpClient);

    login(credentials: Credentials): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, credentials).pipe(
            tap((response) => this.setToken(response.token))
        );

    }




    logout() {
        this.setToken(null);
    }


    getToken(): string | null {
        return this.tokenSignal();
    }

    private setToken(token: string | null) {
        this.tokenSignal.set(token);
        if (token) {
            localStorage.setItem(TOKEN_KEY, token);
        } else {
            localStorage.removeItem(TOKEN_KEY);
        }

    }




}












// solicitud login ----> servidor ----> respuesta token --------> alamcenar token en localStorage

// Solicotud get (token) ----> servidor ----> validar token --------> responde get

// Servicio: clase sin UI
// Signal: 

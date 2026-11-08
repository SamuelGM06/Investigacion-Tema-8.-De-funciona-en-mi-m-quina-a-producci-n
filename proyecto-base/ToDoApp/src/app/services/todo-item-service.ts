import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { NewTodoItem } from '../models/new-todo-item.model';
import { TodoItem } from '../models/todo-item.model';
import { UpdateTodoItem } from '../models/update-todo-item.model';

@Service()
export class TodoItemService {
    private readonly baseUrl = `${environment.apiUrl}/todoitems`;
    private readonly http = inject(HttpClient);

    getAll(): Observable<TodoItem[]> {
        return this.http.get<TodoItem[]>(this.baseUrl);
    }


    toggleComplete(id: number): Observable<TodoItem> {
        return this.http.patch<TodoItem>(`${this.baseUrl}/${id}/toggle`, {});
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    create(todo: NewTodoItem): Observable<TodoItem> {
        return this.http.post<TodoItem>(this.baseUrl, todo);
    }

    getById(id: number): Observable<TodoItem> {
        return this.http.get<TodoItem>(`${this.baseUrl}/${id}`);
    }

    update(id: number, todo: UpdateTodoItem): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${id}`, todo);
    }


}
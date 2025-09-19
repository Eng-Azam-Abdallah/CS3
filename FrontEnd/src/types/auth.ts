export interface LoginCredentials {
    email: string;
    password: string;
}

export interface User {
    id: string;
    username: string;
    email: string;
    isEmailVerified: boolean;
}

export interface AuthResponse {
    user: User;
    token: string;
}

export interface AuthContextType {
    user: User | null;
    token: string | null;
    isAuthenticated: boolean;
    login: (credentials: LoginCredentials) => Promise<void>;
    logout: () => void;
    register: (data: RegisterData) => Promise<void>;
}

export interface RegisterData extends LoginCredentials {
    username: string;
    confirmPassword: string;
}
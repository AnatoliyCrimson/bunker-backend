import React from 'react';
import ErrorPage from '../../pages/ErrorPage';

class ErrorBoundary extends React.Component {
    constructor(props) {
        super(props);
        this.state = { hasError: false };
    }

    // Этот метод срабатывает при ошибке и обновляет стейт, чтобы показать запасной UI
    static getDerivedStateFromError(error) {
        return { hasError: true };
    }

    // Этот метод используется для логирования ошибки (в консоль или внешний сервис типа Sentry)
    componentDidCatch(error, errorInfo) {
        console.error("Uncaught error:", error, errorInfo);
    }

    render() {
        if (this.state.hasError) {
            // Если произошла ошибка, рендерим нашу страницу ошибки
            return (
                <ErrorPage 
                    status="Oops!" 
                    title="Что-то пошло не так" 
                    message="Произошла программная ошибка. Попробуйте вернуться на главную, и сообщить о ней в поддержку. Если не удается это сделать, значит мы уже знаем о ошибке и активно исправляем ее"
                />
            );
        }

        // Если ошибок нет — рендерим дочерние компоненты (то есть всё наше приложение)
        return this.props.children;
    }
}

export default ErrorBoundary;
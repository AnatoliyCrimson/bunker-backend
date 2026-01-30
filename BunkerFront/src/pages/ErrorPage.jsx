import { Link } from "react-router";
import "../styles/pages/Error.scss"

function ErrorPage({status = "404", title="Страница не найдена", message="Кажется, вы заблудились в пустоши. Такого дороги не существует или она был уничтожена"}) {
    
    
    return (
        <>
            <div className="background error-page">
                <div className="error-page__container container">
                    <p className="error-page__message">
                        {message}
                    </p>
                    <h1 className="error-page__status">
                        {status}
                    </h1>
                    <h2 className="error-page__title">
                        {title}
                    </h2>

                    <Link to="/" className="btn error-page__btn">
                        Главная
                    </Link>
                </div>
            </div>
        </>
    );
}

export default ErrorPage;
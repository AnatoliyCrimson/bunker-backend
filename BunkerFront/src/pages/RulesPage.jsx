import "../styles/pages/Rules.scss"

function RulesPage() {
    return (
        <>
            <div className="background background--main rules">
                <div className="container rules__container">
                    <h2 className="rules__title">
                        Правила
                    </h2>
                    <div className="rules__content">
                        <h3 className="rules__title">
                            Цель игры:
                        </h3>
                        <p className="rules__text">
                            Убедить остальных игроков, чтобы Вас взяли в бункер, количество мест в котором ограничено.
                        </p>

                        <h3 className="rules__title">
                            Параметры:
                        </h3>
                        <ul className="rules__list">
                            <li className="rules__item">
                                —Игроки: 4+.
                            </li>
                            <li className="rules__item">
                                —Мест в бункере (N): половина от количества игроков (если их нечётное количество, то округление в меньшую сторону).
                            </li>
                            <li className="rules__item">
                                —Характеристики: у каждого игрока по 9 характеристик.
                            </li>
                            <li className="rules__item">
                                —Голосование: за кандидатов.
                            </li>
                        </ul>

                        <h3 className="rules__title">
                            Ход игры
                        </h3>

                        <p className="rules__text">
                            Игра состоит из последовательных этапов, каждый из которых включает в себя от 3-ёх до 1-го раундов, количество которых уменьшается на 1 в каждом последующем этапе.
                        </p>

                        <h3 className="rules__title">
                            Процесс раунда:
                        </h3>
                        <ul className="rules__list">
                            <li className="rules__item">
                                1) игроки ходят поочереди.

                            </li>
                            <li className="rules__item">
                                2) в свой ход игок открывает 1 из своих характеристик по своему усмотрению.

                            </li>
                            <li className="rules__item">
                                3) он должен аргументировать, как эта характеристика поможет всем выжить в бункере.

                            </li>
                        </ul>

                        <h3 className="rules__title">
                            Процесс 1-го этапа:
                        </h3>     

                        <ul className="rules__list">
                            <li className="rules__item">
                                — Количество раундов: 3.
                            </li>
                            <li className="rules__item">
                                — Голосование (у каждого открыто по 3 характеристики): проводится сразу после 3-го раунда.
                            </li>
                            <li className="rules__item">
                                — каждый игрок голосует за N-1 разных игроков (за себя нельзя). Например, в бункере 3 места, а значит каждый игрок может проголосовать за 2-ух разных игроков.
                            </li>
                            <li className="rules__item">
                                — ценность каждого голоса - 1 балл.
                            </li>
                        </ul>

                        <h3 className="rules__title">
                            Процесс 2-го этапа:
                        </h3>
                        <ul className="rules__list">
                            <li className="rules__item">
                                — Количество раундов: 2.
                            </li>
                            <li className="rules__item">
                                — Голосование (у каждого открыто по 5 характеристик): проводится сразу после 2-го раунда:
                            </li>
                            <li className="rules__item">
                                — каждый игрок голосует за N-1 разных игроков (за себя нельзя).
                            </li>
                            <li className="rules__item">
                                — ценность каждого голоса - 2 балла.
                            </li>
                        </ul>
                        
                        <h3 className="rules__title">
                            Процесс 3-го этапа:
                        </h3>
                        <ul className="rules__list">
                            <li className="rules__item">
                                —Количество раундов: 1;
                            </li>
                            <li className="rules__item">
                                —Голосование (у каждого открыто по 6 характеристик): проводится сразу после раунда:
                            </li>
                            <li className="rules__item">
                                —• каждый игрок голосует за N-1 разных игроков (за себя нельзя);
                            </li>
                            <li className="rules__item">
                                —• ценность каждого голоса - 3 балла.
                            </li>
                        </ul>
                        
                        <h3 className="rules__titel">
                            Процесс дополнительных раундов (от 0 до 3-ёх) задаётся хостом перед началом игры:
                        </h3>
                        <ul className="rules__list">
                            <li className="rules__item">
                                —Количество раундов: 1;
                            </li>
                            <li className="rules__item">
                                —Голосование: проводится сразу после раунда:
                            </li>
                            <li className="rules__item">
                                —• каждый игрок голосует за N-1 разных игроков (за себя нельзя);
                            </li>
                            <li className="rules__item">
                                —• ценность каждого голоса - 3 балла.
                            </li>
                        </ul>
                        
                        <h3 className="rules__title">
                            Окончание игры:
                        </h3>
                        <ul className="rules__list">
                            <li className="rules__item">
                                1) по окончанию каждого голосования каждому игроку давалось количество баллов, зависящих от голосов за них и от раунда;
                            </li>
                            <li className="rules__item">
                                2) игроки сортируются по убыванию итоговых баллов;
                            </li>
                            <li className="rules__item">
                                3) первые N игроков попадают в бункер (если среди N+1 игроков есть 2+ игроков с одинаковым количеством баллов - случайный выбор);
                            </li>
                            <li className="rules__item">
                                4) если не все характеристики открыты, то каждый открывает все оставшиеся;
                            </li>
                            <li className="rules__item">
                                5) подводятся итоги выживут ли люди, попавшие в бункер.
                            </li>
                        </ul>
  
                    </div>          
                </div>
            </div>
        </>
    );
}

export default RulesPage;
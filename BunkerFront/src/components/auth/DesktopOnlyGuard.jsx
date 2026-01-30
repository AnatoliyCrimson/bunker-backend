import { useState, useEffect } from 'react';
import MobileStub from '../../pages/MobileStub';

function DesktopOnlyGuard({ children }) {
    // Начальное состояние
    const [isMobile, setIsMobile] = useState(window.innerWidth < 1124); // 1024px - чтобы отсечь и планшеты тоже

    useEffect(() => {
        const handleResize = () => {
            // Если ширина меньше 1024px - считаем мобильным
            setIsMobile(window.innerWidth < 1024);
        };

        // Слушаем изменение размера окна
        window.addEventListener('resize', handleResize);

        // Чистим слушатель при размонтировании
        return () => window.removeEventListener('resize', handleResize);
    }, []);

    // Если мобилка — показываем заглушку
    if (isMobile) {
        return <MobileStub />;
    }

    // Если десктоп — показываем контент (приложение)
    return children;
}

export default DesktopOnlyGuard;
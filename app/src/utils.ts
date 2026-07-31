
export function openGoogleMaps(
    lat: number,
    lon: number,
    name?: string
) {
    window.open(
        `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(name ?? `${lat},${lon}`)}`,
        '_blank',
        'noopener,noreferrer'
    );

};

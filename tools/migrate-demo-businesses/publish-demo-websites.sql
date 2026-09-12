-- Marks each demo business's website as published, and points its template's
-- own PreviewWebsiteUrl at the same live GitHub Pages deployment. Both fields
-- exist for a real merchant flow (WebsiteTemplateRequest closing) that demo
-- businesses never go through, so they're set directly here instead.
START TRANSACTION;

UPDATE businesses SET WebsiteUrl = 'https://templates.merchforge.site/fashion-template/'
  WHERE Id = 'b6770f8b-14a1-49ee-9c07-929e826e82ad'; -- Fashion-01
UPDATE businesses SET WebsiteUrl = 'https://templates.merchforge.site/fashion-template-02/'
  WHERE Id = 'c1ce50bf-e95a-4c8a-ba49-921b4356c2e8'; -- Fashion-02
UPDATE businesses SET WebsiteUrl = 'https://templates.merchforge.site/electronic-template/'
  WHERE Id = '27e95606-b479-413a-8a89-a9e4cbb24571'; -- Electronics-01
UPDATE businesses SET WebsiteUrl = 'https://templates.merchforge.site/electronic-template-02/'
  WHERE Id = 'df3a1c47-81b3-4a12-92dc-112ba236453d'; -- PhoneCase Co
UPDATE businesses SET WebsiteUrl = 'https://templates.merchforge.site/grocery-template/'
  WHERE Id = '2bad4bc8-68dc-45e8-89a3-27da2a777acd'; -- Green Basket Market

UPDATE website_templates SET PreviewWebsiteUrl = 'https://templates.merchforge.site/fashion-template/'
  WHERE Id = 'e1000000-0000-4000-8000-000000000001'; -- fashion-template-01
UPDATE website_templates SET PreviewWebsiteUrl = 'https://templates.merchforge.site/fashion-template-02/'
  WHERE Id = '0ae75380-3aa6-4282-be2a-442bef306ed9'; -- fashion-template-02
UPDATE website_templates SET PreviewWebsiteUrl = 'https://templates.merchforge.site/electronic-template/'
  WHERE Id = 'e1000000-0000-4000-8000-000000000002'; -- electronic-template-01
UPDATE website_templates SET PreviewWebsiteUrl = 'https://templates.merchforge.site/electronic-template-02/'
  WHERE Id = '8497d9d0-9a0d-45ac-913f-fd3e8fa37237'; -- electronic-template-02
UPDATE website_templates SET PreviewWebsiteUrl = 'https://templates.merchforge.site/grocery-template/'
  WHERE Id = '3c246807-9131-4d87-92a4-49dd78cd1b88'; -- grocery-template-01

COMMIT;

SELECT Name, WebsiteUrl FROM businesses WHERE IsDemo = 1;
SELECT Name, PreviewWebsiteUrl FROM website_templates;
